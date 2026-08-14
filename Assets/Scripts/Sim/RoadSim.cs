using System;
using System.Collections.Generic;

namespace KombiRush.Sim
{
    /// <summary>
    /// The whole run: road generation, kombi motion, collisions, fares. No engine types,
    /// no wall-clock, no Random - given (config, upgrades, seed, input) it always plays out
    /// identically, which is what makes it testable outside Unity.
    /// </summary>
    public sealed class RoadSim
    {
        public readonly SimConfig Cfg;
        public readonly UpgradeSet Up;
        private readonly Rng _rng;

        private readonly List<Entity> _entities = new List<Entity>(96);
        private readonly List<SimEvent> _events = new List<SimEvent>(16);
        private readonly List<RowAudit> _audit = new List<RowAudit>(256);

        private int _nextId = 1;
        private float _nextRowY;
        private float _nextStopY;
        private int _prevFreeMask;
        private float _slowTimer;
        private float _fareAccrualCents;

        // --- kombi state ------------------------------------------------------
        public float PlayerY { get; private set; }
        public float LaneF { get; private set; }
        public int TargetLane { get; private set; }
        public float Speed { get; private set; }
        public float Fuel { get; private set; }
        public int Hull { get; private set; }
        public int Riders { get; private set; }
        public int BankedFareCents { get; private set; }
        public int CashCents { get; private set; }
        public float Combo { get; private set; }
        public float ElapsedSeconds { get; private set; }

        // --- run tallies ------------------------------------------------------
        public int PassengersServed { get; private set; }
        public int CoinsCollected { get; private set; }
        public int StopsServed { get; private set; }
        public float BestCombo { get; private set; }
        public bool IsOver { get; private set; }
        public EndReason Reason { get; private set; }

        public IReadOnlyList<Entity> Entities => _entities;
        public IReadOnlyList<SimEvent> Events => _events;
        /// <summary>Per-row record of what was generated - used by the fairness tests.</summary>
        public IReadOnlyList<RowAudit> Audit => _audit;

        public struct RowAudit
        {
            public float Y;
            public int BlockedMask;
            public int FreeMask;
            public float SpawnedAtPlayerY;
            public float ElapsedSeconds;
        }

        public RoadSim(SimConfig cfg, UpgradeSet upgrades, uint seed)
        {
            Cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            Up = upgrades ?? new UpgradeSet();
            _rng = new Rng(seed);

            Fuel = Up.FuelCapacity;
            Hull = Up.HullMax;
            Combo = 1f;
            BestCombo = 1f;
            TargetLane = Cfg.LaneCount / 2;
            LaneF = TargetLane;
            Speed = Cfg.StartSpeed;
            _prevFreeMask = FullMask;
            _nextRowY = Cfg.MinSpawnDistance + 6f;
            _nextStopY = Cfg.StopIntervalMetres;
        }

        private int FullMask => (1 << Cfg.LaneCount) - 1;

        public float FuelCapacity => Up.FuelCapacity;
        public int HullMax => Up.HullMax;
        public int SeatCapacity => Up.SeatCapacity;
        public float DistanceMetres => PlayerY;

        // ---------------------------------------------------------------------
        // input
        // ---------------------------------------------------------------------
        public void SetTargetLane(int lane)
        {
            if (lane < 0) lane = 0;
            if (lane > Cfg.LaneCount - 1) lane = Cfg.LaneCount - 1;
            TargetLane = lane;
        }

        public void Nudge(int direction) => SetTargetLane(TargetLane + (direction < 0 ? -1 : 1));

        // ---------------------------------------------------------------------
        // main step
        // ---------------------------------------------------------------------
        public void Step(float dt)
        {
            if (IsOver || dt <= 0f) return;
            _events.Clear();

            ElapsedSeconds += dt;
            if (_slowTimer > 0f) _slowTimer -= dt;

            Speed = CurrentSpeed();
            PlayerY += Speed * dt;

            // lateral slide toward the target lane
            float laneStep = Cfg.LaneChangeSpeed * Up.LaneChangeMultiplier * dt;
            float delta = TargetLane - LaneF;
            if (delta > laneStep) LaneF += laneStep;
            else if (delta < -laneStep) LaneF -= laneStep;
            else LaneF = TargetLane;

            // fares accrue per passenger per metre carried
            if (Riders > 0) _fareAccrualCents += Riders * Speed * dt * Cfg.FareCentsPerPassengerMetre;

            Fuel -= Cfg.FuelDrainPerSecond * dt;
            if (Fuel <= 0f)
            {
                Fuel = 0f;
                End(EndReason.OutOfFuel);
                return;
            }

            ScheduleAhead();
            ResolveCollisions();
            Cull();
        }

        private float CurrentSpeed()
        {
            float rampT = Clamp01(ElapsedSeconds / Cfg.SpeedRampSeconds);
            float top = Cfg.TopSpeed * Up.TopSpeedMultiplier;
            float speed = Cfg.StartSpeed + (top - Cfg.StartSpeed) * rampT;
            if (_slowTimer > 0f)
            {
                float factor = Cfg.PotholeSlowFactor + Up.PotholeSlowRelief;
                if (factor > 1f) factor = 1f;
                speed *= factor;
            }
            return speed;
        }

        public float DifficultyT => Clamp01(ElapsedSeconds / Cfg.DifficultyRampSeconds);

        // ---------------------------------------------------------------------
        // generation
        // ---------------------------------------------------------------------
        private void ScheduleAhead()
        {
            float horizon = PlayerY + Cfg.LookAheadMetres;
            int guard = 0;
            while (_nextRowY < horizon && guard++ < 64)
            {
                float y = Math.Max(_nextRowY, PlayerY + Cfg.MinSpawnDistance);
                SpawnRow(y);
                float interval = Cfg.RowIntervalStart + (Cfg.RowIntervalFloor - Cfg.RowIntervalStart) * DifficultyT;
                float gap = Math.Max(7f, Speed * interval);
                _nextRowY = y + gap;
            }

            guard = 0;
            while (_nextStopY < horizon && guard++ < 8)
            {
                float y = Math.Max(_nextStopY, PlayerY + Cfg.MinSpawnDistance);
                int lane = FirstFreeLaneNear(y, _rng.NextInt(Cfg.LaneCount));
                if (lane >= 0) Spawn(EntityKind.Stop, lane, 1, y);
                _nextStopY += Cfg.StopIntervalMetres;
            }
        }

        private void SpawnRow(float y)
        {
            int maxBlocked = 1 + (int)(DifficultyT * (Cfg.LaneCount - 2));
            if (maxBlocked > Cfg.LaneCount - 1) maxBlocked = Cfg.LaneCount - 1;
            int wanted = _rng.NextInt(1, maxBlocked + 1);

            int blockedMask = 0;
            int placed = 0;
            int attempts = 0;
            while (placed < wanted && attempts++ < 12)
            {
                EntityKind kind = PickObstacleKind();
                int span = kind == EntityKind.Roadblock ? 2 : 1;
                if (placed + span > Cfg.LaneCount - 1) { kind = EntityKind.Pothole; span = 1; }

                int lane = _rng.NextInt(Cfg.LaneCount - span + 1);
                int mask = SpanMask(lane, span);
                if ((blockedMask & mask) != 0) continue;               // already occupied
                if ((blockedMask | mask) == FullMask) continue;        // never wall off the road

                blockedMask |= mask;
                placed += span;
                Spawn(kind, lane, span, y);
            }

            int freeMask = FullMask & ~blockedMask;
            freeMask = EnsureReachable(freeMask, y);

            _audit.Add(new RowAudit
            {
                Y = y,
                BlockedMask = FullMask & ~freeMask,
                FreeMask = freeMask,
                SpawnedAtPlayerY = PlayerY,
                ElapsedSeconds = ElapsedSeconds
            });
            _prevFreeMask = freeMask;

            SprinklePickups(freeMask, y);
        }

        /// <summary>
        /// The kombi can only slide so far between rows. If none of this row's free lanes are
        /// reachable from the previous row's free lanes, open the nearest lane back up by
        /// deleting the obstacle sitting in it. Guarantees every generated row is survivable.
        /// </summary>
        private int EnsureReachable(int freeMask, float y)
        {
            int reach = ReachableLanes(_prevFreeMask);
            if ((freeMask & reach) != 0) return freeMask;

            int best = -1, bestDist = int.MaxValue;
            for (int lane = 0; lane < Cfg.LaneCount; lane++)
            {
                if ((reach & (1 << lane)) == 0) continue;
                int dist = NearestSetDistance(freeMask, lane);
                if (dist < bestDist) { bestDist = dist; best = lane; }
            }
            if (best < 0) best = 0;

            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                Entity e = _entities[i];
                if (!e.Alive || !e.IsObstacle) continue;
                if (Math.Abs(e.Y - y) > 0.01f) continue;
                if (!e.BlocksLane(best)) continue;
                e.Alive = false;
                _entities[i] = e;
            }
            return freeMask | (1 << best);
        }

        /// <summary>Lanes the kombi could be in by the time it reaches the next row.</summary>
        private int ReachableLanes(int fromMask)
        {
            float interval = Cfg.RowIntervalStart + (Cfg.RowIntervalFloor - Cfg.RowIntervalStart) * DifficultyT;
            int shift = (int)Math.Floor(Cfg.LaneChangeSpeed * Up.LaneChangeMultiplier * interval * Cfg.ReachSafetyFactor);
            if (shift < 1) shift = 1;

            int reach = 0;
            for (int lane = 0; lane < Cfg.LaneCount; lane++)
            {
                if ((fromMask & (1 << lane)) == 0) continue;
                for (int d = -shift; d <= shift; d++)
                {
                    int l = lane + d;
                    if (l >= 0 && l < Cfg.LaneCount) reach |= 1 << l;
                }
            }
            return reach == 0 ? FullMask : reach;
        }

        private int NearestSetDistance(int mask, int lane)
        {
            int best = int.MaxValue;
            for (int l = 0; l < Cfg.LaneCount; l++)
            {
                if ((mask & (1 << l)) == 0) continue;
                int d = Math.Abs(l - lane);
                if (d < best) best = d;
            }
            return best == int.MaxValue ? Cfg.LaneCount : best;
        }

        private EntityKind PickObstacleKind()
        {
            float total = Cfg.WeightPothole + Cfg.WeightTraffic + Cfg.WeightRoadblock;
            float roll = _rng.NextFloat() * total;
            if (roll < Cfg.WeightPothole) return EntityKind.Pothole;
            if (roll < Cfg.WeightPothole + Cfg.WeightTraffic) return EntityKind.Traffic;
            return EntityKind.Roadblock;
        }

        private void SprinklePickups(int freeMask, float y)
        {
            if (freeMask == 0) return;

            if (_rng.Chance(Cfg.ChancePassengerRow))
            {
                int lane = PickFreeLane(freeMask);
                if (lane >= 0) Spawn(EntityKind.Passenger, lane, 1, y + 1.5f);
            }
            if (_rng.Chance(Cfg.ChanceCoinRow))
            {
                int lane = PickFreeLane(freeMask);
                if (lane >= 0) Spawn(EntityKind.Coin, lane, 1, y - 2.0f);
            }
            if (_rng.Chance(Cfg.ChanceFuelRow))
            {
                int lane = PickFreeLane(freeMask);
                if (lane >= 0) Spawn(EntityKind.FuelCan, lane, 1, y + 3.0f);
            }
        }

        private int PickFreeLane(int freeMask)
        {
            int count = 0;
            for (int l = 0; l < Cfg.LaneCount; l++) if ((freeMask & (1 << l)) != 0) count++;
            if (count == 0) return -1;
            int pick = _rng.NextInt(count);
            for (int l = 0; l < Cfg.LaneCount; l++)
            {
                if ((freeMask & (1 << l)) == 0) continue;
                if (pick-- == 0) return l;
            }
            return -1;
        }

        private int FirstFreeLaneNear(float y, int preferred)
        {
            for (int d = 0; d < Cfg.LaneCount; d++)
            {
                int lane = (preferred + d) % Cfg.LaneCount;
                bool blocked = false;
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity e = _entities[i];
                    if (!e.Alive || !e.IsObstacle) continue;
                    if (Math.Abs(e.Y - y) > 4f) continue;
                    if (e.BlocksLane(lane)) { blocked = true; break; }
                }
                if (!blocked) return lane;
            }
            return -1;
        }

        private int SpanMask(int lane, int span)
        {
            int mask = 0;
            for (int i = 0; i < span; i++) mask |= 1 << (lane + i);
            return mask;
        }

        private void Spawn(EntityKind kind, int lane, int span, float y)
        {
            _entities.Add(new Entity
            {
                Id = _nextId++,
                Kind = kind,
                Lane = lane,
                Span = span,
                Y = y,
                Alive = true,
                Consumed = false
            });
        }

        // ---------------------------------------------------------------------
        // collisions
        // ---------------------------------------------------------------------
        private void ResolveCollisions()
        {
            float playerX = LaneF * Cfg.LaneWidth;

            for (int i = 0; i < _entities.Count; i++)
            {
                Entity e = _entities[i];
                if (!e.Alive || e.Consumed) continue;

                float halfLen = Cfg.KombiHalfLength + (e.IsObstacle ? Cfg.ObstacleHalfLength : Cfg.PickupHalfLength);
                if (Math.Abs(e.Y - PlayerY) >= halfLen) continue;

                float entityX = (e.Lane + (e.Span - 1) * 0.5f) * Cfg.LaneWidth;
                float halfWidth = Cfg.KombiHalfWidth
                                  + (e.IsObstacle ? Cfg.ObstacleHalfWidth : Cfg.PickupHalfWidth)
                                  + (e.Span - 1) * Cfg.LaneWidth * 0.5f;
                if (Math.Abs(entityX - playerX) >= halfWidth) continue;

                e.Consumed = true;
                if (e.Kind != EntityKind.Stop) e.Alive = false;
                _entities[i] = e;
                Apply(e);
                if (IsOver) return;
            }
        }

        private void Apply(Entity e)
        {
            switch (e.Kind)
            {
                case EntityKind.Pothole:
                case EntityKind.Traffic:
                case EntityKind.Roadblock:
                {
                    int damage = e.Kind == EntityKind.Pothole ? Cfg.PotholeDamage
                        : e.Kind == EntityKind.Traffic ? Cfg.TrafficDamage : Cfg.RoadblockDamage;
                    Hull -= damage;
                    if (e.Kind == EntityKind.Pothole) _slowTimer = Cfg.PotholeSlowSeconds;

                    if (Riders > 0 && Cfg.FareLossOnHit > 0f)
                    {
                        int lost = (int)(BankedFareCents * Cfg.FareLossOnHit);
                        BankedFareCents -= lost;
                        if (BankedFareCents < 0) BankedFareCents = 0;
                    }
                    Combo = 1f;

                    Emit(SimEventKind.Hit, e.Kind, damage, 0f, e.Lane);
                    if (Hull <= 0) { Hull = 0; End(EndReason.Wrecked); }
                    break;
                }
                case EntityKind.Passenger:
                {
                    if (Riders < SeatCapacity)
                    {
                        Riders++;
                        BankedFareCents += Cfg.FareCentsPerPassenger;
                        Emit(SimEventKind.PassengerBoarded, e.Kind, Riders, 0f, e.Lane);
                    }
                    else
                    {
                        Emit(SimEventKind.SeatsFull, e.Kind, Riders, 0f, e.Lane);
                    }
                    break;
                }
                case EntityKind.Coin:
                {
                    CashCents += Cfg.CoinCents;
                    CoinsCollected++;
                    Emit(SimEventKind.CoinCollected, e.Kind, Cfg.CoinCents, 0f, e.Lane);
                    break;
                }
                case EntityKind.FuelCan:
                {
                    Fuel += Cfg.FuelPerCan;
                    if (Fuel > FuelCapacity) Fuel = FuelCapacity;
                    Emit(SimEventKind.FuelCollected, e.Kind, (int)Cfg.FuelPerCan, Fuel, e.Lane);
                    break;
                }
                case EntityKind.Stop:
                {
                    if (Riders <= 0) break;
                    int payout = (int)((BankedFareCents + _fareAccrualCents) * Combo);
                    CashCents += payout;
                    PassengersServed += Riders;
                    StopsServed++;
                    Emit(SimEventKind.Payout, e.Kind, payout, Combo, e.Lane);

                    Riders = 0;
                    BankedFareCents = 0;
                    _fareAccrualCents = 0f;
                    Combo += Cfg.ComboStep;
                    if (Combo > Cfg.ComboMax) Combo = Cfg.ComboMax;
                    if (Combo > BestCombo) BestCombo = Combo;
                    break;
                }
            }
        }

        private void Cull()
        {
            float cutoff = PlayerY - 12f;
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                Entity e = _entities[i];
                if (e.Alive && e.Y >= cutoff) continue;
                _entities.RemoveAt(i);
            }
        }

        private void Emit(SimEventKind kind, EntityKind entity, int intValue, float floatValue, int lane)
        {
            _events.Add(new SimEvent { Kind = kind, Entity = entity, IntValue = intValue, FloatValue = floatValue, Lane = lane });
        }

        private void End(EndReason reason)
        {
            IsOver = true;
            Reason = reason;
            Emit(SimEventKind.GameOver, EntityKind.Stop, (int)reason, 0f, TargetLane);
        }

        public RunResult BuildResult()
        {
            return new RunResult
            {
                DistanceMetres = PlayerY,
                CashCents = CashCents,
                PassengersServed = PassengersServed,
                CoinsCollected = CoinsCollected,
                StopsServed = StopsServed,
                BestCombo = BestCombo,
                DurationSeconds = ElapsedSeconds,
                Reason = Reason
            };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
