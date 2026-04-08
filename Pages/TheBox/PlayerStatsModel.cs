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
        public long PurchasedFollowers { get; private set; } = 0;
        public double Influence { get; private set; } = 0;
        public long Altars { get; private set; } = 0;
        public long Prophets { get; private set; } = 0;
        public long Grace { get; private set; } = 0;
        public long Sanctity { get; private set; } = 0;

        private readonly EconomyParams _pietyParams = new EconomyParams();
        private readonly EconomyParams _zealParams = new EconomyParams();

        private readonly EconomyParams _followersParams = new EconomyParams
        {
            BaseCost = 200,
            RoundTo = 5,
            InflationEveryLevels = 10,
            InflationMultiplier = 1.10
        };

        private readonly EconomyParams _altarsParams = new EconomyParams
        {
            BaseCost = 125,
            RoundTo = 10,
            InflationEveryLevels = 5,
            InflationMultiplier = 1.15
        };

        private readonly EconomyParams _prophetsParams = new EconomyParams
        {
            BaseCost = 500,
            RoundTo = 25,
            InflationEveryLevels = 5,
            InflationMultiplier = 1.20
        };

        private readonly EconomyParams _graceParams = new EconomyParams
        {
            BaseCost = 300,
            RoundTo = 10,
            InflationEveryLevels = 8,
            InflationMultiplier = 1.12
        };

        private readonly EconomyParams _sanctityParams = new EconomyParams
        {
            BaseCost = 500,
            RoundTo = 25,
            InflationEveryLevels = 5,
            InflationMultiplier = 1.15
        };

        public PassiveParams Passive { get; } = new PassiveParams
        {
            BaseRatePerFollower = 2.0,
            Alpha = 1.05,
            ZealPerLevel = 2.00
        };

        public LifeForceParams LifeForce { get; } = new LifeForceParams();
        public AltarParams AltarConfig { get; } = new AltarParams();
        public ProphetParams ProphetConfig { get; } = new ProphetParams();

        // Life force state
        private double _currentHp;
        public double CurrentHp => _currentHp;
        public double EffectiveMaxHp => LifeForce.MaxHp + Sanctity * LifeForce.SanctityHpPerLevel;
        public double HpPercent => EffectiveMaxHp > 0 ? _currentHp / EffectiveMaxHp : 1.0;

        // Drain tracking for floating combat text
        public double LastTickDrain { get; private set; } = 0;
        public double LastTickRestore { get; private set; } = 0;
        public bool HasUnlockedLifeForce { get; private set; } = false;
        public bool IsLifeForceDepleted => HasUnlockedLifeForce && _currentHp <= 0;
        public bool ShowSacrificeButton => HasUnlockedLifeForce
            && (Altars > 0 ? IsLifeForceDepleted : HpPercent <= LifeForce.SacrificeAppearPct)
            && Followers >= LifeForce.SacrificeFollowerCost
            && !IsSacrificing;

        // Sacrifice drain state
        public bool IsSacrificing { get; private set; } = false;
        public int SacrificeTicksRemaining { get; private set; } = 0;

        // Milestone state
        public bool HasPerformedFirstSacrifice { get; private set; } = false;
        public HashSet<string> CompletedMilestones { get; private set; } = new();

        public Clicks Clicks { get; }

        // Passive tick state
        private DateTime _lastTickUtc = DateTime.UtcNow;
        private double _passiveRemainder = 0;

        // Altar/Prophet tick accumulators
        private double _altarTickAccumulator = 0;
        private double _prophetTickAccumulator = 0;

        public bool HasUnlockedStatsWindow { get; private set; } = false;
        public bool HasUnlockedFollowerStatsWindow => Piety >= 5;
        public bool HasUnlockedZeal { get; private set; } = false;
        public bool HasUnlockedAltars => HasPerformedFirstSacrifice;
        public bool HasUnlockedProphets => CompletedMilestones.Contains("the-church");
        public bool HasUnlockedGrace => CompletedMilestones.Contains("divine-favor");
        public bool HasUnlockedSanctity => CompletedMilestones.Contains("consecration");

        public int ClickValueBase { get; set; } = 1;
        public BigInteger ClickValue => Clicks.ClickValue(ClickValueBase, Piety);

        public event Action? OnChange;

        public BigInteger PietyCost => Economy.HybridCost((int)Piety, _pietyParams);
        public BigInteger ZealCost => Economy.HybridCost((int)Zeal, _zealParams);
        public BigInteger FollowersCost => Economy.HybridCost((int)PurchasedFollowers, _followersParams);
        public BigInteger AltarsCost => Economy.HybridCost((int)Altars, _altarsParams);
        public BigInteger ProphetsCost => Economy.HybridCost((int)Prophets, _prophetsParams);
        public BigInteger GraceCost => Economy.HybridCost((int)Grace, _graceParams);
        public BigInteger SanctityCost => Economy.HybridCost((int)Sanctity, _sanctityParams);

        private void NotifyStateChanged() => OnChange?.Invoke();

        public PlayerStatsModel()
        {
            Clicks = new Clicks();
            _currentHp = EffectiveMaxHp;

            _pietyParams.BaseCost = StatsUnlock;
            Economy.FitHybrid(_pietyParams, 600, 5000);

            _zealParams.BaseCost = 500;
            Economy.FitHybrid(_zealParams, 5000, 10000);

            Economy.FitHybrid(_followersParams, 1200, 5000);
            Economy.FitHybrid(_altarsParams, 1500, 8750);
            Economy.FitHybrid(_prophetsParams, 5000, 30000);
            Economy.FitHybrid(_graceParams, 3000, 18000);
            Economy.FitHybrid(_sanctityParams, 6000, 35000);

            Clicks.FitCurve(p1: 1, m1: 2.0, p2: 10, m2: 12.0);
            Clicks.SoftcapStart = 36;
            Clicks.SoftcapExponent = 0.7;
        }

        public void IncrementDevotion()
        {
            // No devotion gain when life force is depleted
            if (IsLifeForceDepleted)
            {
                NotifyStateChanged();
                return;
            }

            Devotion += ClickValue;
            if (!HasUnlockedStatsWindow && Devotion >= StatsUnlock)
            {
                HasUnlockedStatsWindow = true;
            }
            if (HasUnlockedLifeForce)
            {
                double restore = LifeForce.HpPerClick;
                _currentHp = Math.Min(EffectiveMaxHp, _currentHp + restore);
                LastTickRestore += restore;
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
            PurchasedFollowers++;
            CheckFollowerUnlocks();
            NotifyStateChanged();
        }

        private void CheckFollowerUnlocks()
        {
            if (!HasUnlockedLifeForce && Followers >= LifeForce.UnlockAtFollowers)
                HasUnlockedLifeForce = true;
            if (!HasUnlockedZeal && Followers >= 5)
                HasUnlockedZeal = true;
        }

        public void BuyAltar()
        {
            if (!HasUnlockedAltars || Devotion < AltarsCost)
                return;
            Devotion -= AltarsCost;
            Altars++;
            NotifyStateChanged();
        }

        public void BuyProphet()
        {
            if (!HasUnlockedProphets || Devotion < ProphetsCost)
                return;
            Devotion -= ProphetsCost;
            Prophets++;
            NotifyStateChanged();
        }

        public void BuyGrace()
        {
            if (!HasUnlockedGrace || Devotion < GraceCost)
                return;
            Devotion -= GraceCost;
            Grace++;
            NotifyStateChanged();
        }

        public void BuySanctity()
        {
            if (!HasUnlockedSanctity || Devotion < SanctityCost)
                return;
            Devotion -= SanctityCost;
            Sanctity++;
            // Max HP increased but current HP stays the same — bar "shrinks"
            NotifyStateChanged();
        }

        public double GetDevotionPerSecond(double globalMultiplier = 1.0, Func<long, double>? diminishingReturns = null)
        {
            if (Followers == 0 && Altars == 0) return 0;
            // Altars contribute as equivalent followers for devotion generation
            double effectiveFollowers = Followers + Altars * AltarConfig.DevotionFollowerEquivalent;
            double baseTerm = Passive.BaseRatePerFollower * Math.Pow(effectiveFollowers, Passive.Alpha);
            double zealTerm = Math.Pow(1.0 + Passive.ZealPerLevel * Math.Max(0, Zeal), Passive.ZealExponent);
            double dr = diminishingReturns?.Invoke(Followers) ?? 1.0;
            return baseTerm * zealTerm * dr * Math.Max(1.0, globalMultiplier);
        }

        public static Func<long, double> MakeDR(double k) => f => 1.0 / (1.0 + f / k);

        public void PerformSacrifice()
        {
            if (!ShowSacrificeButton)
                return;
            HasPerformedFirstSacrifice = true;
            IsSacrificing = true;
            SacrificeTicksRemaining = (int)LifeForce.SacrificeFollowerCost;
            NotifyStateChanged();
        }

        /// <summary>
        /// Called each game tick during sacrifice drain.
        /// Returns true if a follower was consumed this tick, false if drain is complete.
        /// </summary>
        public bool TickSacrifice()
        {
            if (!IsSacrificing || SacrificeTicksRemaining <= 0)
                return false;

            Followers = Math.Max(0, Followers - 1);
            PurchasedFollowers = Math.Max(0, PurchasedFollowers - 1);
            SacrificeTicksRemaining--;

            // Restore HP per follower consumed
            double sacRestore = EffectiveMaxHp * LifeForce.SacrificeHpPerFollowerPct;
            _currentHp = Math.Min(EffectiveMaxHp, _currentHp + sacRestore);
            LastTickRestore += sacRestore;

            if (SacrificeTicksRemaining <= 0)
                IsSacrificing = false;

            NotifyStateChanged();
            return true;
        }

        // Milestone completion
        public void CompleteMilestone(string id, long devotionCost)
        {
            if (CompletedMilestones.Contains(id))
                return;
            if (Devotion < devotionCost)
                return;
            Devotion -= devotionCost;
            CompletedMilestones.Add(id);

            // Run the milestone's OnComplete action
            var milestone = MilestoneRegistry.All.Find(m => m.Id == id);
            milestone?.OnComplete(this);

            NotifyStateChanged();
        }

        /// <summary>Called by milestone OnComplete actions to set internal flags.</summary>
        public void SetMilestoneFlag(string id)
        {
            // Flags are driven by CompletedMilestones.Contains() checks
            // This method exists so MilestoneRegistry can trigger side effects
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
            CheckFollowerUnlocks();
            NotifyStateChanged();
        }

        public void Debug_AddZeal(long amount)
        {
            Zeal = Math.Max(0, Zeal + amount);
            NotifyStateChanged();
        }

        public void Debug_SetHpPercent(double pct)
        {
            _currentHp = Math.Clamp(pct, 0.0, 1.0) * EffectiveMaxHp;
            NotifyStateChanged();
        }

        public void Debug_AddAltars(long amount)
        {
            Altars = Math.Max(0, Altars + amount);
            NotifyStateChanged();
        }

        public void Debug_AddProphets(long amount)
        {
            Prophets = Math.Max(0, Prophets + amount);
            NotifyStateChanged();
        }

        public void Debug_AddGrace(long amount)
        {
            Grace = Math.Max(0, Grace + amount);
            NotifyStateChanged();
        }

        public void Debug_AddSanctity(long amount)
        {
            Sanctity = Math.Max(0, Sanctity + amount);
            NotifyStateChanged();
        }

        public void Debug_AddInfluence(double amount)
        {
            Influence = Math.Max(0, Influence + amount);
            NotifyStateChanged();
        }

        public void Debug_SetFirstSacrifice()
        {
            HasPerformedFirstSacrifice = true;
            NotifyStateChanged();
        }

        #endregion

        private void TickLifeForce(double dt)
        {
            if (!HasUnlockedLifeForce)
                return;

            double baseDrain = LifeForce.BaseDecayPerSecond + Followers * LifeForce.DecayPerFollower;
            double graceReduction = 1.0 / (1.0 + Grace * LifeForce.GraceReductionFactor);
            double decay = baseDrain * graceReduction * dt;
            _currentHp = Math.Max(0, _currentHp - decay);
            LastTickDrain += decay;
        }

        private void TickAltars(double dt)
        {
            if (Altars <= 0 || !HasUnlockedLifeForce)
                return;

            // Only activate when HP is critically low
            if (HpPercent > AltarConfig.ActivationThresholdPct)
            {
                _altarTickAccumulator = 0;
                return;
            }

            // More altars = faster cycle (diminishing: 3s, 1.5s, 1s, 0.75s, ...)
            double effectiveCycle = AltarConfig.CycleTimeSeconds / Altars;

            _altarTickAccumulator += dt;
            if (_altarTickAccumulator < effectiveCycle)
                return;

            _altarTickAccumulator -= effectiveCycle;

            // Altars consume 1 follower per cycle (not per altar), but never drop below safety floor
            long available = Math.Max(0, Followers - AltarConfig.MinFollowersToKeep);
            long toConsume = Math.Min(AltarConfig.FollowersPerAltarPerCycle, available);

            if (toConsume <= 0)
                return;

            Followers -= toConsume;
            PurchasedFollowers = Math.Max(0, PurchasedFollowers - toConsume);
            // HP restore scales with altar count
            double restorePct = AltarConfig.HpPerFollowerBasePct + Math.Max(0, Altars - 1) * AltarConfig.HpPerFollowerScalingPct;
            double altarRestore = toConsume * EffectiveMaxHp * restorePct;
            _currentHp = Math.Min(EffectiveMaxHp, _currentHp + altarRestore);
            LastTickRestore += altarRestore;
        }

        private void TickProphets(double dt)
        {
            if (Prophets <= 0)
                return;

            // Diminishing returns: each prophet shaves off less time
            // At 5 prophets: 5.0 - 1.0 = 4.0s. Scales via log curve.
            double reduction = Math.Log(1 + Prophets) * (1.0 / Math.Log(6));
            double effectiveCycle = Math.Max(
                ProphetConfig.MinCycleTimeSeconds,
                ProphetConfig.BaseCycleTimeSeconds - reduction
            );

            _prophetTickAccumulator += dt;
            if (_prophetTickAccumulator < effectiveCycle)
                return;

            _prophetTickAccumulator -= effectiveCycle;

            // Always recruit 1 follower per cycle
            Followers += 1;
            CheckFollowerUnlocks();

            // Prophets generate influence
            Influence += Prophets * ProphetConfig.InfluencePerProphetPerCycle;
        }

        public void TickPassive(double globalMultiplier = 1.0, Func<long, double>? dr = null)
        {
            var now = DateTime.UtcNow;
            double dt = (now - _lastTickUtc).TotalSeconds;
            if (dt > 1.0) dt = 1.0; // clamp for stability
            _lastTickUtc = now;

            LastTickDrain = 0;
            LastTickRestore = 0;

            TickLifeForce(dt);
            TickAltars(dt);
            TickProphets(dt);

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
