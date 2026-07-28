# CLAUDE.md - Project Guidelines

## Vision
We are building a Water Margin (水浒传) themed Dou Di Zhu (Fight the Landlord) card game, inspired by the gameplay mechanics of Legends of the Three Kingdoms (三国杀) but set in the Water Margin universe. Always remember this: every feature, UI, and design decision should serve this vision. The final product is a polished, Water Margin themed card game on Steam with multiplayer and character skills.

## Game Style & Design Direction

### Art Style
- **Chinese ink wash painting (水墨画)** aesthetic throughout the game
- Card artwork features circular framed illustrations with gold borders, dark aged parchment backgrounds, and gold ink brush calligraphy for rank numbers
- Each card rank has a unique Water Margin themed illustration (tavern, spear, boat, war horse, bow, war drum, tiger, dao blade, gathering hall, loyalty banner, general armor, celestial dragon, Liangshan fortress)
- Red Joker: golden dragon with blazing sun, red/gold color scheme
- Black Joker: fierce tiger with crescent moon, silver/black color scheme
- Overall color palette: dark tones, gold accents, warm earth colors

### UI Layout Reference
- A concept art image is saved at `Assets/Resources/概念图.png`. All UI layout discussions and implementations should reference this concept for guidance. It does not need to be replicated exactly, but it provides a clear vision for the game's layout:
  - **Left/Right AI players**: circular avatar + name + card count, compact and close together
  - **Center table area**: played cards displayed in the middle for all players
  - **Bottom area**: player's hand cards spread out, action buttons above the hand
  - **Top area**: game info, messages

### Gameplay Inspiration
- **Core gameplay**: Dou Di Zhu (Fight the Landlord) card mechanics - bidding, combos, landlord vs farmers
- **Style inspiration**: Legends of the Three Kingdoms (三国杀) visual and mechanical style, adapted to Water Margin setting

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
Phase 1 - Classic single-player Dou Di Zhu with Water Margin visual style (playable against AI).

### Phase Roadmap
1. **Phase 1** - Classic Dou Di Zhu with Water Margin card artwork and themed UI (current)
2. **Phase 2** - Main menu, polished game background, styled bidding/play UI

## Author
jianweisde
