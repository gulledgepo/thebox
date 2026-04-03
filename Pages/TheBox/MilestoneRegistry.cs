using System.Collections.Generic;

namespace PaxstonProject.Pages.TheBox
{
    public static class MilestoneRegistry
    {
        public static List<MilestoneDefinition> All { get; } = new()
        {
            new MilestoneDefinition
            {
                Id = "the-church",
                Name = "The Church",
                Emoji = "⛪",
                RequirementText = "Acquire 5 Altars",
                MaskedText = "Acquire 5 ????",
                DevotionCost = 5000,
                RequirementMet = s => s.Altars >= 5,
                IsRevealed = s => s.HasUnlockedAltars,
                OnComplete = s => s.SetMilestoneFlag("the-church")
            }
        };
    }
}
