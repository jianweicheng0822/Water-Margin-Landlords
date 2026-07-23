# CLAUDE.md - Project Guidelines

## Project
Water Margin Landlords - A 2D Dou Di Zhu (Fight the Landlord) card game built with Unity + C#.
Target platform: Steam (future). Currently focusing on single-player vs AI.

## Development Rules

1. **Discuss before coding.** Always discuss the approach with the user before writing any code. Break large steps into small, incremental sub-steps. Write a small piece, then discuss the next move.
2. **No Co-Authored-By.** Never include `Co-Authored-By` lines in git commit messages.
3. **English comments.** All code must have proper English comments explaining the logic.
4. **English commits.** All git commit messages must be written in English.
5. **Incremental development.** Do not implement large chunks at once. Keep each step small and reviewable.

## Architecture

All scripts use Unity + C# (MonoBehaviour, ScriptableObject, etc. where appropriate).

- **Core (Scripts/Core/):** Card data, deck, combo types, and rule validation.
- **GameFlow (Scripts/GameFlow/):** Turn management, bidding, and game state transitions.
- **AI (Scripts/AI/):** Rule-based AI opponents.
- **UI (Scripts/UI/):** Display and interaction layer.
- **Common (Scripts/Common/):** Shared utilities, events, constants.

## Scene & UI Approach
- All development is done in VS Code (user does not use Unity Editor for scene building).
- UI and scene objects are created programmatically via code (GameSetup.cs).
- Card and UI elements are generated at runtime, not via manual Prefab/scene editing.
- Future: gradually migrate code-generated objects to proper Prefabs as needed.

## Tech Stack
- Unity (URP, 2D)
- C#
- Git for version control
- Steamworks.NET (future)
- Multiplayer networking solution TBD (future)

## Current Phase
Phase 1 - Classic single-player Dou Di Zhu (playable against AI).

## Author
jianweisde
