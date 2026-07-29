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

### UI Layout Principles
- Each player's info card should be positioned near their played cards area
- AI players' info cards sit above their respective play zones (upper-left / upper-right quadrants)
- Human player's info card sits at the bottom-left, next to the hand area
- Center of screen is reserved for played cards and game messages
- Consistent margins and alignment between info cards, card backs, and play areas

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

## Google Flow (AI Image Generation) Guide

When Claude needs a visual asset that cannot be created through code alone, ask the user to generate it with Google Flow. Claude provides the prompt, the user generates and places the file.

### When to use Google Flow
- **Hero portraits/avatars** - 林冲, 鲁智深, 宋江 etc. character art for player profiles
- **New card artwork** - if current card faces need redesign or new card types are added
- **UI decorative elements** - ornamental frames, skill icons, role badges (地主/农民)
- **Menu backgrounds** - main menu, settings screen, loading screen
- **Effect sprites** - bomb explosion, rocket fire, victory/defeat banners
- **Card back variants** - themed card back designs for different game modes

### How to write effective prompts
1. **Always specify the art style**: "Chinese ink wash painting style" (水墨画) to maintain visual consistency
2. **Specify the color palette**: "dark tones, gold accents, warm earth colors, dark brown/amber background"
3. **Specify the resolution**: e.g. "1920x1080" for backgrounds, square for icons/avatars
4. **State what to exclude**: "no text, no characters" (for backgrounds), "no background" (for icons)
5. **Reference the game theme**: "Water Margin (水浒传)", "Song Dynasty aesthetic"
6. **For UI elements**: specify "dark atmosphere suitable for overlay" so assets work on the game's dark UI

### File placement
- Card sprites → `Assets/Resources/Sprites/card_*.jpeg`
- Backgrounds → `Assets/Resources/Sprites/background*.jpeg`
- Hero avatars → `Assets/Resources/Sprites/hero_*.png`
- UI elements → `Assets/Resources/Sprites/ui_*.png`
- Keep filenames in English, lowercase with underscores

### Successful prompt examples
- **Card artwork**: "Chinese ink wash painting, circular frame with gold border, [subject] illustration, dark aged parchment background, gold ink brush calligraphy, Water Margin theme, 512x768"
- **Game background**: "Chinese ink wash painting style game table background, dark wooden table surface with subtle wood grain texture, faded water ink landscape painting border decorations, mountains and rivers in mist, Song Dynasty aesthetic, Water Margin theme, warm amber and dark brown tones, subtle golden ornamental corner borders, top-down view, 1920x1080, dark atmosphere, no text, no characters"

## Audio Assets
- Current BGM (`Assets/Resources/Audio/menu_bgm.mp3`) is generated by Suno AI, for non-commercial testing only. Replace with licensed music or subscribe to Suno membership before commercial release.

## Author
jianweisde
