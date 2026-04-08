namespace PaxstonProject.Pages.TheBox
{
    public class LifeForceParams
    {
        public double MaxHp { get; set; } = 1000.0;
        public double BaseDecayPerSecond { get; set; } = 8.0;

        // Additional decay per follower owned
        public double DecayPerFollower { get; set; } = 1.5;

        // Grace: each level reduces drain multiplicatively. Formula: drain * 1/(1 + Grace * factor)
        // Grace 1 = ~17% reduction, Grace 5 = ~50%, Grace 10 = ~67%
        public double GraceReductionFactor { get; set; } = 0.2;

        // Sanctity: each level adds this much to max HP
        public double SanctityHpPerLevel { get; set; } = 100.0;

        // Clicking restores a small amount of HP
        public double HpPerClick { get; set; } = 2.0;

        // Unlock gate
        public long UnlockAtFollowers { get; set; } = 5;

        // Whisper shift thresholds (HP percentage 0.0-1.0)
        public double DesperatePoolStartPct { get; set; } = 0.65;
        public double DarkPoolStartPct { get; set; } = 0.30;

        // Sacrifice mechanic
        public double SacrificeAppearPct { get; set; } = 0.15;
        public long SacrificeFollowerCost { get; set; } = 5;
        // HP restored per follower consumed (as % of max HP)
        public double SacrificeHpPerFollowerPct { get; set; } = 0.10;
    }
}
