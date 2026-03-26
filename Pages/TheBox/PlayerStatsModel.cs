using System.Numerics;
using PaxstonProject.Pages.TheBox;

namespace PaxstonProject.Pages.TheBox
{
    public class PlayerStatsModel
    {
        public int StatsUnlock { get; private set; } = 50;
        public BigInteger Devotion { get; private set; } = BigInteger.Zero;
        public long Piety { get; private set; } = 0;
        public long Zeal { get; private set; } = 0;
        public long Followers { get; private set; } = 0;

        private readonly EconomyParams _pietyParams = new EconomyParams();
        private readonly EconomyParams _zealParams = new EconomyParams();

        private readonly EconomyParams _followersParams = new EconomyParams
        {
            BaseCost = 50,
            RoundTo = 5,
            InflationEveryLevels = 10,
            InflationMultiplier = 1.10
        };

        public PassiveParams Passive { get; } = new PassiveParams
        {
            BaseRatePerFollower = 2.0,
            Alpha = 1.05,
            ZealPerLevel = 2.00
        };

        public Clicks Clicks { get; }

        // Passive tick state
        private DateTime _lastTickUtc = DateTime.UtcNow;
        private double _passiveRemainder = 0;

        public bool HasUnlockedStatsWindow { get; private set; } = false;
        public bool HasUnlockedFollowerStatsWindow => Piety >= 5;
        public bool HasUnlockedZeal => Followers >= 5;

        public int ClickValueBase { get; set; } = 1;
        public BigInteger ClickValue => Clicks.ClickValue(ClickValueBase, Piety);

        public event Action? OnChange;

        public BigInteger PietyCost => Economy.HybridCost((int)Piety, _pietyParams);
        public BigInteger ZealCost => Economy.HybridCost((int)Zeal, _zealParams);
        public BigInteger FollowersCost => Economy.HybridCost((int)Followers, _followersParams);

        private void NotifyStateChanged() => OnChange?.Invoke();

        public PlayerStatsModel()
        {
            Clicks = new Clicks();

            _pietyParams.BaseCost = StatsUnlock;
            Economy.FitHybrid(_pietyParams, 600, 5000);

            _zealParams.BaseCost = 500;
            Economy.FitHybrid(_zealParams, 5000, 10000);

            Economy.FitHybrid(_followersParams, 600, 3500);

            Clicks.FitCurve(p1: 1, m1: 2.0, p2: 10, m2: 12.0);
            Clicks.SoftcapStart = 36;
            Clicks.SoftcapExponent = 0.7;
        }

        public void IncrementDevotion()
        {
            Devotion += ClickValue;
            if (!HasUnlockedStatsWindow && Devotion >= StatsUnlock)
            {
                HasUnlockedStatsWindow = true;
            }
            NotifyStateChanged();
        }

        public void SacrificeForPiety()
        {
            if (Devotion < PietyCost)
                return;
            Devotion -= PietyCost;
            Piety++;
            NotifyStateChanged();
        }

        public void SacrificeForZeal()
        {
            if (!HasUnlockedZeal || Devotion < ZealCost)
                return;
            Devotion -= ZealCost;
            Zeal++;
            NotifyStateChanged();
        }

        public void BuyFollower()
        {
            var cost = FollowersCost;
            if (Devotion < cost)
                return;
            Devotion -= cost;
            Followers++;
            NotifyStateChanged();
        }

        public double GetDevotionPerSecond(double globalMultiplier = 1.0, Func<long, double>? diminishingReturns = null)
        {
            if (Followers == 0) return 0;
            double baseTerm = Passive.BaseRatePerFollower * Math.Pow(Followers, Passive.Alpha);
            double zealTerm = Math.Pow(1.0 + Passive.ZealPerLevel * Math.Max(0, Zeal), Passive.ZealExponent);
            double dr = diminishingReturns?.Invoke(Followers) ?? 1.0;
            return baseTerm * zealTerm * dr * Math.Max(1.0, globalMultiplier);
        }

        public static Func<long, double> MakeDR(double k) => f => 1.0 / (1.0 + f / k);

        public void TickPassive(double globalMultiplier = 1.0, Func<long, double>? dr = null)
        {
            var now = DateTime.UtcNow;
            double dt = (now - _lastTickUtc).TotalSeconds;
            if (dt > 1.0) dt = 1.0; // clamp for stability
            _lastTickUtc = now;

            double dps = GetDevotionPerSecond(globalMultiplier, dr);
            double gain = dps * dt + _passiveRemainder;
            long whole = (long)Math.Floor(gain);
            Devotion += whole;
            _passiveRemainder = gain - whole;
            NotifyStateChanged();
        }
    }
}
