# The Box — Project Guidelines

## Design Philosophy

1. **Polish matters.** This project is a representation of Paxston as a developer. If there's a cool animation or visual touch we can add, do it — but never at the cost of performance.

2. **Loading & transitions should feel intentional.** No default template placeholders. Loading states should visually connect to what the app actually looks like.

## Technical Notes

- Blazor WebAssembly (.NET 8), client-side SPA
- Dark theme with cyan (#4fc3f7) accents
- Game math uses curve-fitting rather than hand-curated tables
- BigInteger for Devotion to handle unbounded growth
- All game logic classes live in `PaxstonProject.Pages.TheBox` namespace
