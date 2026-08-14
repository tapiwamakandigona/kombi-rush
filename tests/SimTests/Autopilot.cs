using System;
using KombiRush.Sim;

namespace KombiRush.Tests
{
    /// <summary>
    /// A deliberately simple driver used as a playability probe: it scores each lane by how much
    /// clear road it has, adds a small pull toward pickups, and refuses any move whose path
    /// crosses traffic it cannot clear in time. If this bot cannot survive a couple of minutes,
    /// the road generator is producing traffic a human could not read either.
    /// </summary>
    public static class Autopilot
    {
        // the bot only reacts to what a player can actually see on a portrait screen
        private const float Horizon = 28f;

        public static void Decide(RoadSim sim)
        {
            int lanes = sim.Cfg.LaneCount;
            var clear = new float[lanes];
            var bonus = new float[lanes];
            for (int l = 0; l < lanes; l++) clear[l] = Horizon;

            // an obstacle level with the kombi is still lethal, so only ignore it once it is
            // fully past the collision window
            float behindCutoff = -(sim.Cfg.KombiHalfLength + sim.Cfg.TrafficHalfLength + 0.6f);

            var entities = sim.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                Entity e = entities[i];
                if (!e.Alive) continue;
                float ahead = e.Y - sim.PlayerY;
                if (ahead < behindCutoff || ahead > Horizon) continue;

                if (e.IsObstacle)
                {
                    for (int l = e.Lane; l < e.Lane + e.Span && l < lanes; l++)
                        if (ahead < clear[l]) clear[l] = ahead;
                }
                else if (ahead < 2f) continue;
                else if (e.Kind == EntityKind.Passenger) bonus[e.Lane] += sim.Riders < sim.SeatCapacity ? 7f : 0f;
                else if (e.Kind == EntityKind.FuelCan) bonus[e.Lane] += sim.Fuel < 20f ? 16f : 4f;
                else if (e.Kind == EntityKind.Coin) bonus[e.Lane] += 3f;
                else if (e.Kind == EntityKind.Stop) bonus[e.Lane] += sim.Riders > 0 ? 12f : 0f;
            }

            float laneSpeed = sim.Cfg.LaneChangeSpeed * sim.Up.LaneChangeMultiplier;
            int best = -1;
            float bestScore = float.MinValue;

            for (int l = 0; l < lanes; l++)
            {
                float lanesToCross = Math.Abs(l - sim.LaneF);
                float crossSeconds = lanesToCross / laneSpeed;
                // road covered while sliding across, plus a safety margin
                float needed = sim.Speed * crossSeconds + sim.Cfg.KombiHalfLength + sim.Cfg.TrafficHalfLength + 1.5f;

                // every lane the kombi passes through must be clear for the whole crossing
                bool pathSafe = true;
                int from = (int)Math.Floor(Math.Min(sim.LaneF, l));
                int to = (int)Math.Ceiling(Math.Max(sim.LaneF, l));
                for (int k = from; k <= to && pathSafe; k++)
                {
                    if (k < 0 || k >= lanes) continue;
                    if (k == (int)Math.Round(sim.LaneF) && k == l) continue; // staying put is judged below
                    if (clear[k] < needed) pathSafe = false;
                }
                if (l == (int)Math.Round(sim.LaneF) && clear[l] < needed) pathSafe = false;

                float score = Math.Min(clear[l], Horizon) * 3f + bonus[l] - lanesToCross * 2.5f;
                if (!pathSafe) score -= 500f;
                if (score > bestScore) { bestScore = score; best = l; }
            }

            if (best >= 0) sim.SetTargetLane(best);
        }

        /// <summary>Runs a full auto-played run at a fixed step. Returns the result.</summary>
        public static RunResult Play(uint seed, float maxSeconds = 240f, UpgradeSet upgrades = null, float dt = 1f / 60f)
        {
            var sim = new RoadSim(SimConfig.Default(), upgrades ?? new UpgradeSet(), seed);
            int steps = (int)(maxSeconds / dt);
            for (int i = 0; i < steps && !sim.IsOver; i++)
            {
                if (i % 4 == 0) Decide(sim);   // ~15 decisions per second, like a human
                sim.Step(dt);
            }
            return sim.BuildResult();
        }
    }
}
