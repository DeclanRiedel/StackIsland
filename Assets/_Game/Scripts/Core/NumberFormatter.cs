using System;
using System.Globalization;

namespace StackIsland
{
    /// <summary>Formats big numbers: 999, 1.23K, 45.6M, 789B, 1.2T, then aa, ab, ac...</summary>
    public static class NumberFormatter
    {
        static readonly string[] Named = { "", "K", "M", "B", "T" };
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "-∞";
            string sign = value < 0 ? "-" : "";
            value = Math.Abs(value);

            if (value < 1000)
            {
                // Whole numbers for currency below 1000 (we never show 12.000001 coins).
                return sign + Math.Floor(value + 1e-9).ToString("0", Inv);
            }

            int tier = (int)Math.Floor(Math.Log10(value) / 3.0);
            double scaled = value / Math.Pow(1000, tier);
            string text = ThreeDigits(scaled);
            if (text == "1000")
            {
                tier++;
                text = "1";
            }
            return sign + text + Suffix(tier);
        }

        /// <summary>Rates like "+1.5/s" need decimals for small values.</summary>
        public static string FormatRate(double perSecond)
        {
            if (perSecond <= 0) return "0";
            if (perSecond < 10) return perSecond.ToString("0.0", Inv);
            return Format(perSecond);
        }

        static string ThreeDigits(double scaled)
        {
            // Truncate (not round) so we never show more money than the player has.
            if (scaled >= 100) return Math.Floor(scaled).ToString("0", Inv);
            if (scaled >= 10) return (Math.Floor(scaled * 10) / 10).ToString("0.#", Inv);
            return (Math.Floor(scaled * 100) / 100).ToString("0.##", Inv);
        }

        public static string Suffix(int tier)
        {
            if (tier < Named.Length) return Named[tier];
            int i = tier - Named.Length;            // 0 -> "aa", 1 -> "ab"...
            char a = (char)('a' + (i / 26) % 26);
            char b = (char)('a' + i % 26);
            return new string(new[] { a, b });
        }

        public static string FormatDuration(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
