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
            },
            new MilestoneDefinition
            {
                Id = "divine-favor",
                Name = "Divine Favor",
                Emoji = "🕊️",
                RequirementText = "Acquire 15 Piety",
                MaskedText = "Acquire 15 ????",
                DevotionCost = 2000,
                RequirementMet = s => s.Piety >= 15,
                IsRevealed = s => s.HasUnlockedStatsWindow,
                OnComplete = s => s.SetMilestoneFlag("divine-favor")
            },
            new MilestoneDefinition
            {
                Id = "consecration",
                Name = "Consecration",
                Emoji = "🛡️",
                RequirementText = "Acquire 10 Grace",
                MaskedText = "Acquire 10 ????",
                DevotionCost = 8000,
                RequirementMet = s => s.Grace >= 10,
                IsRevealed = s => s.HasUnlockedGrace,
                OnComplete = s => s.SetMilestoneFlag("consecration")
            }
        };
    }
}
