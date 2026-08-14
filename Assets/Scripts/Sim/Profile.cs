using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KombiRush.Sim
{
    /// <summary>
    /// Persistent player state. Serialised as plain key=value lines: forward compatible
    /// (unknown keys ignored, missing keys keep defaults) and diffable when debugging on device.
    /// </summary>
    public sealed class Profile
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int WalletCents;
        public float BestDistanceMetres;
        public int BestRunCents;
        public int TotalRuns;
        public int TotalPassengers;
        public int LastBonusDay = -1;
        public bool SoundOn = true;
        public UpgradeSet Upgrades = new UpgradeSet();

        public void ApplyRun(RunResult run)
        {
            TotalRuns++;
            WalletCents += run.CashCents;
            TotalPassengers += run.PassengersServed;
            if (run.DistanceMetres > BestDistanceMetres) BestDistanceMetres = run.DistanceMetres;
            if (run.CashCents > BestRunCents) BestRunCents = run.CashCents;
        }

        public bool CanAfford(UpgradeId id)
        {
            if (Upgrades.IsMaxed(id)) return false;
            return WalletCents >= Economy.UpgradeCost(id, Upgrades.Level(id));
        }

        /// <summary>Buys one level. Returns false (and changes nothing) when maxed or too poor.</summary>
        public bool Buy(UpgradeId id)
        {
            if (!CanAfford(id)) return false;
            int cost = Economy.UpgradeCost(id, Upgrades.Level(id));
            WalletCents -= cost;
            Upgrades.SetLevel(id, Upgrades.Level(id) + 1);
            return true;
        }

        /// <summary>Daily bonus, paid once per calendar day. dayIndex is days since epoch.</summary>
        public int ClaimDailyBonus(int dayIndex, int bonusCents = 150)
        {
            if (dayIndex == LastBonusDay) return 0;
            LastBonusDay = dayIndex;
            WalletCents += bonusCents;
            return bonusCents;
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("version=").Append(Version).Append('\n');
            sb.Append("wallet=").Append(WalletCents).Append('\n');
            sb.Append("bestDistance=").Append(BestDistanceMetres.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("bestRun=").Append(BestRunCents).Append('\n');
            sb.Append("runs=").Append(TotalRuns).Append('\n');
            sb.Append("passengers=").Append(TotalPassengers).Append('\n');
            sb.Append("bonusDay=").Append(LastBonusDay).Append('\n');
            sb.Append("sound=").Append(SoundOn ? 1 : 0).Append('\n');
            for (int i = 0; i < UpgradeSet.Count; i++)
            {
                sb.Append("up.").Append(((UpgradeId)i).ToString()).Append('=')
                  .Append(Upgrades.Level((UpgradeId)i)).Append('\n');
            }
            return sb.ToString();
        }

        public static Profile Deserialize(string text)
        {
            var p = new Profile();
            if (string.IsNullOrEmpty(text)) return p;

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq);
                string value = line.Substring(eq + 1);

                switch (key)
                {
                    case "version": p.Version = ParseInt(value, CurrentVersion); break;
                    case "wallet": p.WalletCents = Math.Max(0, ParseInt(value, 0)); break;
                    case "bestDistance": p.BestDistanceMetres = ParseFloat(value, 0f); break;
                    case "bestRun": p.BestRunCents = Math.Max(0, ParseInt(value, 0)); break;
                    case "runs": p.TotalRuns = Math.Max(0, ParseInt(value, 0)); break;
                    case "passengers": p.TotalPassengers = Math.Max(0, ParseInt(value, 0)); break;
                    case "bonusDay": p.LastBonusDay = ParseInt(value, -1); break;
                    case "sound": p.SoundOn = ParseInt(value, 1) != 0; break;
                    default:
                        if (key.StartsWith("up.", StringComparison.Ordinal))
                        {
                            string name = key.Substring(3);
                            for (int i = 0; i < UpgradeSet.Count; i++)
                            {
                                if (((UpgradeId)i).ToString() == name)
                                {
                                    p.Upgrades.SetLevel((UpgradeId)i, ParseInt(value, 0));
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
            return p;
        }

        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        /// <summary>Upgrade rows for the garage screen, in display order.</summary>
        public static IReadOnlyList<UpgradeId> UpgradeOrder { get; } = new[]
        {
            UpgradeId.Engine, UpgradeId.Tyres, UpgradeId.Hull, UpgradeId.FuelTank, UpgradeId.Seats
        };
    }
}
