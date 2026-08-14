namespace KombiRush.Sim
{
    /// <summary>
    /// Every tunable number in the run simulation. Engine-free on purpose: the same config
    /// drives the Unity presentation layer and the headless test harness.
    /// Distances are metres, speeds metres/second, cash is in US cents.
    /// </summary>
    public sealed class SimConfig
    {
        // --- Road -------------------------------------------------------------
        public int LaneCount = 4;
        /// <summary>A real traffic lane, so speeds and sizes below can be real too.</summary>
        public float LaneWidth = 3.2f;

        // --- Kombi motion ----------------------------------------------------
        public float StartSpeed = 8.5f;      // about 31 km/h
        public float TopSpeed = 17f;         // about 61 km/h, fast for a kombi in town
        /// <summary>Seconds of driving to reach TopSpeed (before upgrades).</summary>
        public float SpeedRampSeconds = 150f;
        /// <summary>How fast the kombi slides sideways, in lanes per second.</summary>
        public float LaneChangeSpeed = 2.6f;   // lanes per second - arcade quick, still readable
        public float PotholeSlowFactor = 0.55f;
        public float PotholeSlowSeconds = 1.1f;

        // --- Survival --------------------------------------------------------
        public float FuelDrainPerSecond = 1.0f;
        public float FuelPerCan = 25f;
        public int PotholeDamage = 1;
        public int TrafficDamage = 2;
        public int RoadblockDamage = 2;

        // --- Spawning --------------------------------------------------------
        /// <summary>Rows are scheduled this far ahead of the kombi.</summary>
        public float LookAheadMetres = 64f;
        /// <summary>Seconds between obstacle rows at the start of a run.</summary>
        public float RowIntervalStart = 2.1f;
        /// <summary>Seconds between obstacle rows once fully ramped. Never goes below this.</summary>
        public float RowIntervalFloor = 1.35f;
        public float DifficultyRampSeconds = 180f;
        /// <summary>Most lanes an obstacle row may block once difficulty is maxed.</summary>
        public int MaxBlockedLanesAtPeak = 2;
        /// <summary>Never spawn anything closer to the kombi than this (anti-cheap-death rule).</summary>
        public float MinSpawnDistance = 30f;
        /// <summary>Metres between passenger stops.</summary>
        public float StopIntervalMetres = 180f;
        /// <summary>
        /// How much of the kombi's theoretical sideways reach the generator is allowed to rely on
        /// when it guarantees the next row is survivable. Below 1 it keeps a fairness margin, so a
        /// player never has to slide at the absolute physical limit to find the gap.
        /// </summary>
        public float ReachSafetyFactor = 0.6f;

        // --- Row composition (weights are relative) ---------------------------
        public float WeightPothole = 1.0f;
        public float WeightTraffic = 0.85f;
        public float WeightRoadblock = 0.28f;
        public float ChanceCoinRow = 0.55f;
        public float ChancePassengerRow = 0.40f;
        public float ChanceFuelRow = 0.20f;

        // --- Hitboxes (half-extents, metres) ---------------------------------
        // Real vehicle footprints: a Hiace-shaped kombi is about 1.9m x 4.8m. Lanes are 3.2m, so
        // two vehicles side by side in neighbouring lanes leave roughly 1.4m of daylight - enough
        // to steer through without clipping, tight enough to feel like town traffic.
        public float KombiHalfLength = 2.35f;
        public float KombiHalfWidth = 0.92f;
        public float TrafficHalfLength = 2.15f;
        public float TrafficHalfWidth = 0.86f;
        public float PotholeHalfLength = 0.6f;
        public float PotholeHalfWidth = 0.85f;
        public float RoadblockHalfLength = 0.5f;
        public float RoadblockEdgeInset = 0.25f;
        public float PickupHalfLength = 0.9f;
        public float PickupHalfWidth = 0.8f;

        /// <summary>Half length of an obstacle's hitbox, by kind.</summary>
        public float ObstacleHalfLength(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Traffic: return TrafficHalfLength;
                case EntityKind.Pothole: return PotholeHalfLength;
                case EntityKind.Roadblock: return RoadblockHalfLength;
                case EntityKind.Stop: return 0.8f;
                default: return PickupHalfLength;
            }
        }

        /// <summary>Half width of an obstacle's hitbox, by kind and how many lanes it spans.</summary>
        public float ObstacleHalfWidth(EntityKind kind, int span)
        {
            switch (kind)
            {
                case EntityKind.Traffic: return TrafficHalfWidth;
                case EntityKind.Pothole: return PotholeHalfWidth;
                case EntityKind.Roadblock: return span * LaneWidth * 0.5f - RoadblockEdgeInset;
                case EntityKind.Stop: return span * LaneWidth * 0.5f;
                default: return PickupHalfWidth;
            }
        }

        // --- Economy ---------------------------------------------------------
        /// <summary>Fare banked per passenger when they board.</summary>
        public int FareCentsPerPassenger = 50;
        /// <summary>Extra cents per passenger per metre carried (rounded down at payout).</summary>
        public float FareCentsPerPassengerMetre = 0.06f;
        public int CoinCents = 25;
        /// <summary>Payout combo multiplier grows by this per stop served, capped by ComboMax.</summary>
        public float ComboStep = 0.25f;
        public float ComboMax = 3.0f;
        /// <summary>Fraction of banked fares lost when the kombi takes a hit with riders aboard.</summary>
        public float FareLossOnHit = 0.15f;

        public static SimConfig Default() => new SimConfig();
    }
}
