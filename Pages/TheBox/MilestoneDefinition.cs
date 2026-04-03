using System;

namespace PaxstonProject.Pages.TheBox
{
    public enum MilestoneState
    {
        Locked,
        Available,
        Completed
    }

    public class MilestoneDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Emoji { get; set; } = "❓";
        public string RequirementText { get; set; } = "";
        public string MaskedText { get; set; } = "";
        public long DevotionCost { get; set; } = 0;

        /// <summary>Whether the requirement has been met (e.g., "has 5 altars").</summary>
        public Func<PlayerStatsModel, bool> RequirementMet { get; set; } = _ => false;

        /// <summary>Whether to show real text vs ????. Controls if the referenced stat is known to the player.</summary>
        public Func<PlayerStatsModel, bool> IsRevealed { get; set; } = _ => false;

        /// <summary>Side effects when the milestone is completed.</summary>
        public Action<PlayerStatsModel> OnComplete { get; set; } = _ => { };

        public MilestoneState GetState(PlayerStatsModel stats)
        {
            if (stats.CompletedMilestones.Contains(Id))
                return MilestoneState.Completed;
            if (RequirementMet(stats))
                return MilestoneState.Available;
            return MilestoneState.Locked;
        }

        public string GetDisplayText(PlayerStatsModel stats)
        {
            if (stats.CompletedMilestones.Contains(Id))
                return Name;
            return IsRevealed(stats) ? RequirementText : MaskedText;
        }
    }
}
