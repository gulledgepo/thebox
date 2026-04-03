namespace PaxstonProject.Pages.TheBox
{
    public class AltarParams
    {
        // How often altars fire (seconds)
        public double CycleTimeSeconds { get; set; } = 3.0;

        // Each altar consumes this many followers per cycle
        public int FollowersPerAltarPerCycle { get; set; } = 1;

        // Base HP restored per follower consumed (% of max HP), scales with altar count
        public double HpPerFollowerBasePct { get; set; } = 0.05;

        // Each additional altar adds this much to the restore % (so 5 altars = 0.05 + 4*0.03 = 0.17)
        public double HpPerFollowerScalingPct { get; set; } = 0.03;

        // Altars contribute to passive devotion: each altar = this many "equivalent followers"
        public double DevotionFollowerEquivalent { get; set; } = 1.5;

        // Safety floor: altars never consume below this follower count
        public int MinFollowersToKeep { get; set; } = 1;

        // Altars only fire when HP drops below this percentage
        public double ActivationThresholdPct { get; set; } = 0.05;
    }
}
