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
        public float LaneWidth = 1.6f;

        // --- Kombi motion ----------------------------------------------------
        public float StartSpeed = 9f;
        public float TopSpeed = 24f;
        /// <summary>Seconds of driving to reach TopSpeed (before upgrades).</summary>
        public float SpeedRampSeconds = 150f;
        /// <summary>How fast the kombi slides sideways, in lanes per second.</summary>
        public float LaneChangeSpeed = 4.2f;
        public float PotholeSlowFactor = 0.55f;
        public float PotholeSlowSeconds = 1.1f;

        // --- Survival --------------------------------------------------------
        public float FuelDrainPerSecond = 1.0f;
        public float FuelPerCan = 22f;
        public int PotholeDamage = 1;
        public int TrafficDamage = 2;
        public int RoadblockDamage = 2;

        // --- Spawning --------------------------------------------------------
        /// <summary>Rows are scheduled this far ahead of the kombi.</summary>
        public float LookAheadMetres = 46f;
        /// <summary>Seconds between obstacle rows at the start of a run.</summary>
        public float RowIntervalStart = 1.35f;
        /// <summary>Seconds between obstacle rows once fully ramped. Never goes below this.</summary>
        public float RowIntervalFloor = 0.72f;
        public float DifficultyRampSeconds = 180f;
        /// <summary>Never spawn anything closer to the kombi than this (anti-cheap-death rule).</summary>
        public float MinSpawnDistance = 22f;
        /// <summary>Metres between passenger stops.</summary>
        public float StopIntervalMetres = 300f;
        /// <summary>
        /// How much of the kombi's theoretical sideways reach the generator is allowed to rely on
        /// when it guarantees the next row is survivable. Below 1 it keeps a fairness margin, so a
        /// player never has to slide at the absolute physical limit to find the gap.
        /// </summary>
        public float ReachSafetyFactor = 0.6f;

        // --- Row composition (weights are relative) ---------------------------
        public float WeightPothole = 1.0f;
        public float WeightTraffic = 0.85f;
        public float WeightRoadblock = 0.35f;
        public float ChanceCoinRow = 0.55f;
        public float ChancePassengerRow = 0.32f;
        public float ChanceFuelRow = 0.14f;

        // --- Hitboxes (half-extents, metres) ---------------------------------
        public float KombiHalfLength = 0.95f;
        public float KombiHalfWidth = 0.50f;
        public float ObstacleHalfLength = 0.75f;
        public float ObstacleHalfWidth = 0.55f;
        public float PickupHalfLength = 0.85f;
        public float PickupHalfWidth = 0.62f;

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
