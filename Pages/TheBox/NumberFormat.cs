using System.Numerics;

namespace PaxstonProject.Pages.TheBox
{
    public static class NumberFormat
    {
        private static readonly (BigInteger Threshold, string Suffix)[] Suffixes = {
            (BigInteger.Parse("1000000000000"), "T"),
            (BigInteger.Parse("1000000000"), "B"),
            (BigInteger.Parse("1000000"), "M"),
            (BigInteger.Parse("1000"), "K"),
        };

        public static string Abbreviate(BigInteger value)
        {
            if (value < 10000)
                return value.ToString("N0");

            foreach (var (threshold, suffix) in Suffixes)
            {
                if (value >= threshold)
                {
                    double scaled = (double)value / (double)threshold;
                    return scaled < 100
                        ? $"{scaled:0.#}{suffix}"
                        : $"{scaled:0}{suffix}";
                }
            }

            return value.ToString("N0");
        }
    }
}
