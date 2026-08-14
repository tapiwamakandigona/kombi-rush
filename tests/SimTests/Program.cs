using System;
using System.Collections.Generic;
using KombiRush.Sim;
using static KombiRush.Tests.Harness;

namespace KombiRush.Tests
{
    public static class Program
    {
        private const float Dt = 1f / 60f;

        public static int Main()
        {
            Console.WriteLine("Kombi Rush - simulation tests");
            Console.WriteLine();

            Console.WriteLine("determinism");
            Test("same seed produces an identical run", () =>
            {
                RunResult a = Autopilot.Play(1337u, 120f);
                RunResult b = Autopilot.Play(1337u, 120f);
                Near(a.DistanceMetres, b.DistanceMetres, 0.0001f, "distance must match exactly");
                Equal(a.CashCents, b.CashCents, "cash must match exactly");
                Equal(a.PassengersServed, b.PassengersServed, "passengers must match exactly");
                Equal((int)a.Reason, (int)b.Reason, "end reason must match");
            });

            Test("different seeds produce different roads", () =>
            {
                RunResult a = Autopilot.Play(11u, 90f);
                RunResult b = Autopilot.Play(9999u, 90f);
                True(Math.Abs(a.DistanceMetres - b.DistanceMetres) > 0.5f || a.CashCents != b.CashCents,
                    "two seeds produced identical runs, RNG is not feeding generation");
            });

            Console.WriteLine();
            Console.WriteLine("road generation fairness");
            Test("every generated row leaves at least one open lane", () =>
            {
                int rows = 0;
                for (uint seed = 1; seed <= 40; seed++)
                {
                    var sim = Drive(seed, 200f);
                    var audit = sim.Audit;
                    for (int i = 0; i < audit.Count; i++)
                    {
                        rows++;
                        True(audit[i].FreeMask != 0,
                            "seed " + seed + " row at y=" + audit[i].Y.ToString("0.0") + " blocked every lane");
                    }
                }
                Greater(rows, 1500f, "expected thousands of audited rows, generator may be idle");
                Console.WriteLine("          audited " + rows + " rows across 40 seeds");
            });

            Test("an open lane is always reachable from the previous row", () =>
            {
                for (uint seed = 1; seed <= 40; seed++)
                {
                    var sim = Drive(seed, 200f);
                    var audit = sim.Audit;
                    for (int i = 1; i < audit.Count; i++)
                    {
                        int prevFree = audit[i - 1].FreeMask;
                        int free = audit[i].FreeMask;
                        bool reachable = false;
                        for (int l = 0; l < sim.Cfg.LaneCount && !reachable; l++)
                        {
                            if ((prevFree & (1 << l)) == 0) continue;
                            for (int d = -2; d <= 2 && !reachable; d++)
                            {
                                int t = l + d;
                                if (t < 0 || t >= sim.Cfg.LaneCount) continue;
                                if ((free & (1 << t)) != 0) reachable = true;
                            }
                        }
                        True(reachable, "seed " + seed + " row " + i + " has no reachable open lane");
                    }
                }
            });

            Test("nothing spawns inside the reaction window", () =>
            {
                for (uint seed = 5; seed <= 25; seed++)
                {
                    var sim = new RoadSim(SimConfig.Default(), new UpgradeSet(), seed);
                    var seen = new HashSet<int>();
                    int steps = (int)(150f / Dt);
                    for (int i = 0; i < steps && !sim.IsOver; i++)
                    {
                        if (i % 4 == 0) Autopilot.Decide(sim);
                        sim.Step(Dt);
                        var entities = sim.Entities;
                        for (int k = 0; k < entities.Count; k++)
                        {
                            Entity e = entities[k];
                            if (!seen.Add(e.Id)) continue;
                            if (!e.IsObstacle) continue;
                            float ahead = e.Y - sim.PlayerY;
                            Greater(ahead, sim.Cfg.MinSpawnDistance - 6f,
                                "seed " + seed + " spawned a " + e.Kind + " only " + ahead.ToString("0.0") + "m ahead");
                        }
                    }
                }
            });

            Console.WriteLine();
            Console.WriteLine("playability");
            Test("a simple bot never dies in the first 45s, and usually lasts minutes", () =>
            {
                var times = new List<float>();
                float worstTime = float.MaxValue, worstDist = float.MaxValue;
                uint worstSeed = 0;
                for (uint seed = 1; seed <= 25; seed++)
                {
                    RunResult r = Autopilot.Play(seed, 300f);
                    times.Add(r.DurationSeconds);
                    if (r.DurationSeconds < worstTime) { worstTime = r.DurationSeconds; worstSeed = seed; }
                    if (r.DistanceMetres < worstDist) worstDist = r.DistanceMetres;
                }
                times.Sort();
                float median = times[times.Count / 2];
                Console.WriteLine("          worst seed " + worstSeed + ": " + worstTime.ToString("0.0") + "s / "
                                  + worstDist.ToString("0") + "m, median " + median.ToString("0.0") + "s");
                Greater(worstTime, 45f, "bot died too early - road is unfair");
                Greater(worstDist, 400f, "bot covered too little ground");
                Greater(median, 100f, "median run is too short for a session-based game");
            });

            Test("both pressures kill: some runs end in a wreck, some run dry", () =>
            {
                int wrecked = 0, dry = 0;
                for (uint seed = 1; seed <= 25; seed++)
                {
                    RunResult r = Autopilot.Play(seed, 400f);
                    if (r.Reason == EndReason.Wrecked) wrecked++;
                    else if (r.Reason == EndReason.OutOfFuel) dry++;
                }
                Console.WriteLine("          " + wrecked + " wrecks, " + dry + " ran dry out of 25");
                Greater(wrecked, 0f, "nothing ever wrecks - collisions are not a real threat");
                Greater(dry, 0f, "nothing ever runs dry - fuel is not a real constraint");
                AtMost(dry, 15f, "most runs end on fuel, which reads as arbitrary rather than earned");
            });

            Test("the run still ends - no immortal bot", () =>
            {
                RunResult r = Autopilot.Play(3u, 3600f);
                True(r.Reason != EndReason.None, "a one hour run never ended; fuel or damage pressure is broken");
            });

            Test("upgrades measurably help", () =>
            {
                var stock = new UpgradeSet();
                var maxed = new UpgradeSet();
                for (int i = 0; i < UpgradeSet.Count; i++) maxed.SetLevel((UpgradeId)i, UpgradeSet.MaxLevel);

                float stockDist = 0f, maxedDist = 0f;
                for (uint seed = 1; seed <= 12; seed++)
                {
                    stockDist += Autopilot.Play(seed, 300f, stock).DistanceMetres;
                    maxedDist += Autopilot.Play(seed, 300f, maxed).DistanceMetres;
                }
                Console.WriteLine("          stock " + (stockDist / 12f).ToString("0") + "m vs maxed "
                                  + (maxedDist / 12f).ToString("0") + "m average");
                Greater(maxedDist, stockDist, "a fully upgraded kombi must out-drive a stock one");
            });

            Console.WriteLine();
            Console.WriteLine("run rules");
            Test("fuel never goes negative and ends the run when empty", () =>
            {
                var cfg = SimConfig.Default();
                cfg.ChanceFuelRow = 0f;                 // no refuelling
                var sim = new RoadSim(cfg, new UpgradeSet(), 42u);
                for (int i = 0; i < (int)(600f / Dt) && !sim.IsOver; i++)
                {
                    sim.SetTargetLane(sim.TargetLane);
                    sim.Step(Dt);
                    True(sim.Fuel >= 0f, "fuel went negative");
                }
                True(sim.IsOver, "run never ended without fuel pickups");
            });

            Test("hull damage ends the run and hull never goes below zero", () =>
            {
                var sim = new RoadSim(SimConfig.Default(), new UpgradeSet(), 7u);
                for (int i = 0; i < (int)(600f / Dt) && !sim.IsOver; i++)
                {
                    sim.Step(Dt);                       // no steering at all: crash into everything
                    True(sim.Hull >= 0, "hull went negative");
                }
                True(sim.IsOver, "a kombi that never steers somehow survived");
                Equal((int)EndReason.Wrecked, (int)sim.Reason, "expected a wreck, not " + sim.Reason);
            });

            Test("seats are never oversold", () =>
            {
                var cfg = SimConfig.Default();
                cfg.ChancePassengerRow = 0.9f;
                var sim = new RoadSim(cfg, new UpgradeSet(), 99u);
                for (int i = 0; i < (int)(240f / Dt) && !sim.IsOver; i++)
                {
                    if (i % 4 == 0) Autopilot.Decide(sim);
                    sim.Step(Dt);
                    AtMost(sim.Riders, sim.SeatCapacity, "more riders than seats");
                }
            });

            Test("speed ramps up then holds at the ceiling", () =>
            {
                var sim = new RoadSim(SimConfig.Default(), new UpgradeSet(), 5u);
                float atStart = sim.Speed;
                var samples = new List<float>();
                for (int i = 0; i < (int)(400f / Dt); i++)
                {
                    if (i % 4 == 0) Autopilot.Decide(sim);
                    if (sim.IsOver) break;
                    sim.Step(Dt);
                    if (i % 600 == 0) samples.Add(sim.Speed);
                }
                Near(SimConfig.Default().StartSpeed, atStart, 0.01f, "run must start at StartSpeed");
                foreach (float s in samples)
                    AtMost(s, SimConfig.Default().TopSpeed + 0.01f, "speed exceeded the ceiling");
            });

            Console.WriteLine();
            Console.WriteLine("fares and economy");
            Test("payouts need riders, and the combo grows then caps", () =>
            {
                var cfg = SimConfig.Default();
                var sim = new RoadSim(cfg, new UpgradeSet(), 4u);
                int payouts = 0;
                float maxCombo = 1f;
                for (int i = 0; i < (int)(600f / Dt) && !sim.IsOver; i++)
                {
                    if (i % 4 == 0) Autopilot.Decide(sim);
                    sim.Step(Dt);
                    foreach (SimEvent ev in sim.Events)
                    {
                        if (ev.Kind != SimEventKind.Payout) continue;
                        payouts++;
                        Greater(ev.IntValue, 0f, "a payout paid nothing");
                        if (ev.FloatValue > maxCombo) maxCombo = ev.FloatValue;
                    }
                    AtMost(sim.Combo, cfg.ComboMax, "combo exceeded the cap");
                }
                Console.WriteLine("          " + payouts + " payouts, best combo x" + maxCombo.ToString("0.00"));
                Greater(payouts, 3f, "too few payouts - the board-and-deliver loop is not firing often enough");
            });

            Test("upgrade costs rise with level and stop at max", () =>
            {
                for (int i = 0; i < UpgradeSet.Count; i++)
                {
                    var id = (UpgradeId)i;
                    int previous = 0;
                    for (int level = 0; level < UpgradeSet.MaxLevel; level++)
                    {
                        int cost = Economy.UpgradeCost(id, level);
                        Greater(cost, previous, id + " level " + level + " is not dearer than the one before");
                        previous = cost;
                    }
                    Equal(0, Economy.UpgradeCost(id, UpgradeSet.MaxLevel), id + " still charges at max level");
                }
            });

            Test("buying an upgrade spends the wallet exactly once", () =>
            {
                var p = new Profile();
                int cost = Economy.UpgradeCost(UpgradeId.Tyres, 0);
                p.WalletCents = cost - 1;
                True(!p.Buy(UpgradeId.Tyres), "bought an upgrade without enough money");
                Equal(cost - 1, p.WalletCents, "wallet changed on a failed purchase");
                p.WalletCents = cost;
                True(p.Buy(UpgradeId.Tyres), "could not buy an affordable upgrade");
                Equal(0, p.WalletCents, "wallet not debited correctly");
                Equal(1, p.Upgrades.Level(UpgradeId.Tyres), "upgrade level did not rise");
            });

            Test("cash formats as USD", () =>
            {
                Equal("$0.00", Economy.FormatCents(0), "zero");
                Equal("$1.05", Economy.FormatCents(105), "dollars and cents");
                Equal("$12.30", Economy.FormatCents(1230), "larger amount");
            });

            Console.WriteLine();
            Console.WriteLine("profile persistence");
            Test("profile survives a save/load round trip", () =>
            {
                var p = new Profile { WalletCents = 4321, BestDistanceMetres = 1234.5f, TotalRuns = 9, SoundOn = false };
                p.Upgrades.SetLevel(UpgradeId.Engine, 3);
                p.Upgrades.SetLevel(UpgradeId.Seats, 5);
                Profile back = Profile.Deserialize(p.Serialize());
                Equal(4321, back.WalletCents, "wallet");
                Near(1234.5f, back.BestDistanceMetres, 0.01f, "best distance");
                Equal(9, back.TotalRuns, "runs");
                Equal(3, back.Upgrades.Level(UpgradeId.Engine), "engine level");
                Equal(5, back.Upgrades.Level(UpgradeId.Seats), "seats level");
                True(!back.SoundOn, "sound flag");
            });

            Test("unknown and corrupt save lines are ignored, not fatal", () =>
            {
                Profile p = Profile.Deserialize("wallet=500\nfuture.key=whatever\nnonsense\nbestDistance=abc\nup.Engine=99\n");
                Equal(500, p.WalletCents, "known key still read");
                Near(0f, p.BestDistanceMetres, 0.001f, "unparseable float falls back to default");
                Equal(UpgradeSet.MaxLevel, p.Upgrades.Level(UpgradeId.Engine), "levels clamp to max");
            });

            Test("daily bonus pays once per day", () =>
            {
                var p = new Profile();
                Equal(150, p.ClaimDailyBonus(20000), "first claim");
                Equal(0, p.ClaimDailyBonus(20000), "second claim on the same day");
                Equal(150, p.ClaimDailyBonus(20001), "next day");
                Equal(300, p.WalletCents, "wallet total after two claims");
            });

            Test("a run result feeds the profile bests", () =>
            {
                var p = new Profile();
                p.ApplyRun(new RunResult { DistanceMetres = 900f, CashCents = 700, PassengersServed = 12 });
                p.ApplyRun(new RunResult { DistanceMetres = 400f, CashCents = 300, PassengersServed = 5 });
                Equal(1000, p.WalletCents, "earnings accumulate");
                Near(900f, p.BestDistanceMetres, 0.01f, "best distance keeps the max");
                Equal(700, p.BestRunCents, "best single run keeps the max");
                Equal(17, p.TotalPassengers, "passengers accumulate");
                Equal(2, p.TotalRuns, "runs counted");
            });

            return Summary();
        }

        private static RoadSim Drive(uint seed, float seconds)
        {
            var sim = new RoadSim(SimConfig.Default(), new UpgradeSet(), seed);
            int steps = (int)(seconds / Dt);
            for (int i = 0; i < steps && !sim.IsOver; i++)
            {
                if (i % 4 == 0) Autopilot.Decide(sim);
                sim.Step(Dt);
            }
            return sim;
        }
    }
}
