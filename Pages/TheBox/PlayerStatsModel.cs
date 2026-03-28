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

        public LifeForceParams LifeForce { get; } = new LifeForceParams();

        // Life force state
        private double _currentHp;
        public double CurrentHp => _currentHp;
        public double HpPercent => LifeForce.MaxHp > 0 ? _currentHp / LifeForce.MaxHp : 1.0;
        public bool HasUnlockedLifeForce { get; private set; } = false;
        public bool IsLifeForceDepleted => HasUnlockedLifeForce && _currentHp <= 0;
        public bool ShowSacrificeButton => HasUnlockedLifeForce
            && HpPercent <= LifeForce.SacrificeAppearPct
            && Followers >= LifeForce.SacrificeFollowerCost;

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
            _currentHp = LifeForce.MaxHp;

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
            if (HasUnlockedLifeForce)
            {
                _currentHp = Math.Min(LifeForce.MaxHp, _currentHp + LifeForce.HpPerClick);
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
            if (!HasUnlockedLifeForce && Followers >= LifeForce.UnlockAtFollowers)
            {
                HasUnlockedLifeForce = true;
            }
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

        public void PerformSacrifice()
        {
            if (!ShowSacrificeButton)
                return;
            Followers = Math.Max(0, Followers - LifeForce.SacrificeFollowerCost);
            _currentHp = Math.Min(LifeForce.MaxHp, _currentHp + LifeForce.MaxHp * LifeForce.SacrificeRestorePct);
            NotifyStateChanged();
        }

        #region Debug / Testing

        public void Debug_AddDevotion(long amount)
        {
            Devotion += amount;
            if (!HasUnlockedStatsWindow && Devotion >= StatsUnlock)
                HasUnlockedStatsWindow = true;
            NotifyStateChanged();
        }

        public void Debug_AddPiety(long amount)
        {
            Piety = Math.Max(0, Piety + amount);
            NotifyStateChanged();
        }

        public void Debug_AddFollowers(long amount)
        {
            Followers = Math.Max(0, Followers + amount);
            if (!HasUnlockedLifeForce && Followers >= LifeForce.UnlockAtFollowers)
                HasUnlockedLifeForce = true;
            NotifyStateChanged();
        }

        public void Debug_AddZeal(long amount)
        {
            Zeal = Math.Max(0, Zeal + amount);
            NotifyStateChanged();
        }

        public void Debug_SetHpPercent(double pct)
        {
            _currentHp = Math.Clamp(pct, 0.0, 1.0) * LifeForce.MaxHp;
            NotifyStateChanged();
        }

        #endregion

        private void TickLifeForce(double dt)
        {
            if (!HasUnlockedLifeForce)
                return;

            double decay = (LifeForce.BaseDecayPerSecond + Followers * LifeForce.DecayPerFollower) * dt;
            _currentHp = Math.Max(0, _currentHp - decay);
        }

        public void TickPassive(double globalMultiplier = 1.0, Func<long, double>? dr = null)
        {
            var now = DateTime.UtcNow;
            double dt = (now - _lastTickUtc).TotalSeconds;
            if (dt > 1.0) dt = 1.0; // clamp for stability
            _lastTickUtc = now;

            TickLifeForce(dt);

            if (IsLifeForceDepleted)
            {
                _passiveRemainder = 0;
                NotifyStateChanged();
                return;
            }

            double dps = GetDevotionPerSecond(globalMultiplier, dr);
            double gain = dps * dt + _passiveRemainder;
            long whole = (long)Math.Floor(gain);
            Devotion += whole;
            _passiveRemainder = gain - whole;
            NotifyStateChanged();
        }
    }
}
