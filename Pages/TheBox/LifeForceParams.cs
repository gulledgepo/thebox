namespace PaxstonProject.Pages.TheBox
{
    public class LifeForceParams
    {
        public double MaxHp { get; set; } = 1000.0;
        public double BaseDecayPerSecond { get; set; } = 8.0;

        // Additional decay per follower owned
        public double DecayPerFollower { get; set; } = 1.5;

        // Hook for future purchasable stat to slow decay
        public double DecayReductionPerLevel { get; set; } = 0.05;

        // Clicking restores a small amount of HP
        public double HpPerClick { get; set; } = 2.0;

        // Unlock gate
        public long UnlockAtFollowers { get; set; } = 5;

        // Whisper shift thresholds (HP percentage 0.0-1.0)
        public double DesperatePoolStartPct { get; set; } = 0.65;
        public double DarkPoolStartPct { get; set; } = 0.30;

        // Sacrifice mechanic
        public double SacrificeAppearPct { get; set; } = 0.15;
        public long SacrificeFollowerCost { get; set; } = 3;
        public double SacrificeRestorePct { get; set; } = 0.60;
    }
}
