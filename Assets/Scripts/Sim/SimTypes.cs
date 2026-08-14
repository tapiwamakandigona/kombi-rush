namespace KombiRush.Sim
{
    public enum EntityKind
    {
        Pothole = 0,
        Traffic = 1,
        Roadblock = 2,
        Passenger = 3,
        Coin = 4,
        FuelCan = 5,
        Stop = 6
    }

    public enum EndReason
    {
        None = 0,
        Wrecked = 1,
        OutOfFuel = 2
    }

    public enum SimEventKind
    {
        Hit = 0,
        PassengerBoarded = 1,
        CoinCollected = 2,
        FuelCollected = 3,
        Payout = 4,
        GameOver = 5,
        SeatsFull = 6
    }

    public struct SimEvent
    {
        public SimEventKind Kind;
        public EntityKind Entity;
        public int IntValue;    // cash for Payout, damage for Hit, count for PassengerBoarded
        public float FloatValue; // combo for Payout
        public int Lane;
    }

    /// <summary>One thing on the road. Value type, stored in a pooled list.</summary>
    public struct Entity
    {
        public int Id;
        public EntityKind Kind;
        public int Lane;
        /// <summary>Lanes covered starting at Lane (roadblocks cover 2).</summary>
        public int Span;
        /// <summary>Distance along the road, metres, absolute.</summary>
        public float Y;
        public bool Alive;
        /// <summary>True once the kombi has already resolved a collision with it.</summary>
        public bool Consumed;

        public bool BlocksLane(int lane) => lane >= Lane && lane < Lane + Span;
        public bool IsObstacle => Kind == EntityKind.Pothole || Kind == EntityKind.Traffic || Kind == EntityKind.Roadblock;
        public bool IsPickup => Kind == EntityKind.Passenger || Kind == EntityKind.Coin || Kind == EntityKind.FuelCan;
    }

    public struct RunResult
    {
        public float DistanceMetres;
        public int CashCents;
        public int PassengersServed;
        public int CoinsCollected;
        public int StopsServed;
        public float BestCombo;
        public float DurationSeconds;
        public EndReason Reason;
    }
}
