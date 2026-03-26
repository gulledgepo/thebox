using System;
using System.Numerics;

namespace PaxstonProject.Pages.TheBox
{
    public sealed class EconomyParams
    {
        public long BaseCost { get; set; } = 50;
        public double Power { get; set; } = 0.3957973;
        public double Growth { get; set; } = 1.14493797;

        // Inflation (optional): every N levels, multiply future costs
        public int InflationEveryLevels { get; set; } = 10;
        public double InflationMultiplier { get; set; } = 1.10;

        // Rounding for clean UI (nearest 5, 10, 25…)
        public int RoundTo { get; set; } = 5;

        // Discount synergy (optional): e.g., Sacrifice reduces costs up to 30%
        public double DiscountPerLevel { get; set; } = 0.02;
        public double MaxDiscount { get; set; } = 0.30;
    }

    public static class Economy
    {
        /// Cost curve: base * (level+1)^p * growth^level
        public static BigInteger HybridCost(int level, EconomyParams p, int discountLevels = 0)
        {
            if (level < 0) level = 0;

            // 1) base hybrid curve
            double cost = p.BaseCost
                          * Math.Pow(level + 1, p.Power)
                          * Math.Pow(p.Growth, level);

            // 2) inflation steps (e.g., every 10 levels)
            if (p.InflationEveryLevels > 0 && p.InflationMultiplier > 1.0)
            {
                int steps = level / p.InflationEveryLevels;
                cost *= Math.Pow(p.InflationMultiplier, steps);
            }

            // 3) discount synergy (e.g., Sacrifice)
            double discount = Math.Min(p.DiscountPerLevel * discountLevels, p.MaxDiscount);
            cost *= (1.0 - discount);

            // 4) rounding (UI friendly)
            return RoundTo((BigInteger)Math.Round(cost), p.RoundTo);
        }

        /// Sum of costs from 0..(level-1) – useful for displays/refunds.
        public static BigInteger TotalHybridSpent(int level, EconomyParams p, int discountLevels = 0)
        {
            BigInteger total = 0;
            for (int i = 0; i < level; i++)
                total += HybridCost(i, p, discountLevels);
            return total;
        }

        /// Fit Power & Growth so L10 ~= target10 and L20 ~= target20 for a given base.
        /// (Solves a tiny 2x2 linear system in logs.)
        public static void FitHybrid(EconomyParams p, long targetAt10, long targetAt20)
        {
            double A11 = Math.Log(11.0), A12 = 10.0;
            double A21 = Math.Log(21.0), A22 = 20.0;
            double b1 = Math.Log((double)targetAt10 / p.BaseCost);
            double b2 = Math.Log((double)targetAt20 / p.BaseCost);

            double det = A11 * A22 - A12 * A21;
            double power = (b1 * A22 - A12 * b2) / det;
            double lnGrowth = (A11 * b2 - A21 * b1) / det;

            p.Power = power;
            p.Growth = Math.Exp(lnGrowth);
        }

        private static BigInteger RoundTo(BigInteger value, int step)
            => step <= 1 ? value : (BigInteger)((long)Math.Round((double)value / step) * step);
    }
}
