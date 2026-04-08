namespace PaxstonProject.Pages.TheBox
{
    public class ProphetParams
    {
        // Base cycle time at 1 prophet (seconds)
        public double BaseCycleTimeSeconds { get; set; } = 5.0;

        // Minimum cycle time floor (seconds) — diminishing returns stop here
        public double MinCycleTimeSeconds { get; set; } = 0.5;

        // Always recruits 1 follower per cycle, more prophets = faster cycle

        // Influence generated per prophet per cycle
        public double InfluencePerProphetPerCycle { get; set; } = 1.0;
    }
}
