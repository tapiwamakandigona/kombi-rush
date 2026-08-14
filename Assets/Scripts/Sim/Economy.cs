using System;

namespace KombiRush.Sim
{
    public enum UpgradeId
    {
        FuelTank = 0,
        Hull = 1,
        Seats = 2,
        Tyres = 3,
        Engine = 4
    }

    /// <summary>Upgrade levels owned by the player, plus the stats they derive.</summary>
    public sealed class UpgradeSet
    {
        public const int Count = 5;
        public const int MaxLevel = 5;

        private readonly int[] _levels = new int[Count];

        public int Level(UpgradeId id) => _levels[(int)id];

        public void SetLevel(UpgradeId id, int level)
        {
            _levels[(int)id] = level < 0 ? 0 : (level > MaxLevel ? MaxLevel : level);
        }

        public bool IsMaxed(UpgradeId id) => Level(id) >= MaxLevel;

        public UpgradeSet Clone()
        {
            var copy = new UpgradeSet();
            for (int i = 0; i < Count; i++) copy._levels[i] = _levels[i];
            return copy;
        }

        // --- derived stats -----------------------------------------------------
        public float FuelCapacity => 60f + 14f * Level(UpgradeId.FuelTank);
        public int HullMax => 4 + Level(UpgradeId.Hull);
        public int SeatCapacity => 8 + 2 * Level(UpgradeId.Seats);
        public float LaneChangeMultiplier => 1f + 0.09f * Level(UpgradeId.Tyres);
        /// <summary>Tyres soften the pothole speed penalty (never removes it entirely).</summary>
        public float PotholeSlowRelief => 0.06f * Level(UpgradeId.Tyres);
        public float TopSpeedMultiplier => 1f + 0.055f * Level(UpgradeId.Engine);
    }

    public static class Economy
    {
        /// <summary>Cost in cents to take an upgrade from <paramref name="currentLevel"/> to the next one.</summary>
        public static int UpgradeCost(UpgradeId id, int currentLevel)
        {
            if (currentLevel >= UpgradeSet.MaxLevel) return 0;
            int baseCost = BaseCost(id);
            double scaled = baseCost * Math.Pow(currentLevel + 1, 1.55);
            // round up to the nearest 10c so prices read cleanly in USD
            return (int)(Math.Ceiling(scaled / 10.0) * 10.0);
        }

        public static int BaseCost(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.FuelTank: return 600;
                case UpgradeId.Hull: return 900;
                case UpgradeId.Seats: return 750;
                case UpgradeId.Tyres: return 500;
                case UpgradeId.Engine: return 1100;
                default: return 600;
            }
        }

        public static string DisplayName(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.FuelTank: return "Fuel Tank";
                case UpgradeId.Hull: return "Body Panels";
                case UpgradeId.Seats: return "Extra Seats";
                case UpgradeId.Tyres: return "Tyres";
                case UpgradeId.Engine: return "Engine";
                default: return id.ToString();
            }
        }

        public static string Blurb(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.FuelTank: return "Longer shifts before you have to refuel.";
                case UpgradeId.Hull: return "Takes one more knock before the kombi is finished.";
                case UpgradeId.Seats: return "Carry more passengers, bank bigger fares.";
                case UpgradeId.Tyres: return "Change lanes quicker, shrug off potholes.";
                case UpgradeId.Engine: return "Higher top speed - more distance, more risk.";
                default: return string.Empty;
            }
        }

        /// <summary>Formats cents as USD, the currency Zimbabwean fares are quoted in.</summary>
        public static string FormatCents(int cents)
        {
            int abs = cents < 0 ? -cents : cents;
            string sign = cents < 0 ? "-" : string.Empty;
            return sign + "$" + (abs / 100).ToString() + "." + (abs % 100).ToString("00");
        }
    }
}
