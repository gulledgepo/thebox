using System;
using System.Numerics;

namespace PaxstonProject.Pages.TheBox
{
    public class Clicks
    {
        /// <summary>
        /// Power/exponent for the (piety + 1) term. Tuned via FitCurve.
        /// </summary>
        public double A { get; private set; }

        /// <summary>
        /// Growth multiplier for the G^piety term. Tuned via FitCurve.
        /// </summary>
        public double G { get; private set; }

        /// <summary>
        /// Piety value at which softcap begins. Default: 40.
        /// </summary>
        public int SoftcapStart { get; set; } = 40;

        /// <summary>
        /// Exponent applied to piety above softcap. Default: 0.65.
        /// </summary>
        public double SoftcapExponent { get; set; } = 0.65;

        public Clicks()
        {
            FitCurve(10, 5.0, 20, 15.0); // Sensible defaults
        }

        /// <summary>
        /// Fits A and G so that Piety=p1 yields multiplier m1, and Piety=p2 yields m2.
        /// </summary>
        public void FitCurve(int p1, double m1, int p2, double m2)
        {
            double x1 = Math.Log(p1 + 1);
            double x2 = Math.Log(p2 + 1);
            double y1 = Math.Log(m1);
            double y2 = Math.Log(m2);

            double denom = x1 * p2 - x2 * p1;
            if (Math.Abs(denom) < 1e-8)
            {
                A = 1.0;
                G = 1.0;
                return;
            }

            double Afit = (y1 * p2 - y2 * p1) / denom;
            double lnGfit = (x1 * y2 - x2 * y1) / denom;
            A = Afit;
            G = Math.Exp(lnGfit);
        }

        /// <summary>
        /// Returns the multiplicative Piety multiplier for a given piety value.
        /// Applies softcap if piety >= SoftcapStart.
        /// </summary>
        public double PietyMultiplier(long piety)
        {
            double p = piety;
            if (SoftcapExponent < 1.0 && p >= SoftcapStart)
            {
                double extra = p - SoftcapStart;
                p = SoftcapStart + Math.Pow(extra, SoftcapExponent);
            }
            return Math.Pow(p + 1, A) * Math.Pow(G, p);
        }

        /// <summary>
        /// Computes the final click value as BigInteger, given base click, piety, and optional multipliers.
        /// </summary>
        public BigInteger ClickValue(
            int baseClick,
            long piety,
            double extraMult = 1.0,
            double flatBonus = 0.5)
        {
            double mult = PietyMultiplier(piety) * Math.Max(1.0, extraMult);
            double value = baseClick * mult + flatBonus;
            return new BigInteger(Math.Floor(value));
        }
    }
}
