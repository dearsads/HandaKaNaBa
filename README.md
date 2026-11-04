# Handa Ka Na Ba?

Disaster survival card game for IT111L Concepts in OOP (Laboratory) - Machine Problem.
C# Windows Forms, .NET 8, Visual Studio 2022.

## Opening the project

1. Open `HandaKaNaBa.sln` in Visual Studio 2022 (with the ".NET desktop development" workload).
2. Press F5. The game runs right away using `FakeGameManager` (simple fake rules) so the front end can be tested on Day 1.
3. On Day 2, when `GameManager` is finished, change ONE line in `UI/Forms/MenuForm.cs` > `CreateGame()`:
   `return new GameManager();`

## Folder structure and owners

| Folder / file | What it is | Owner |
|---|---|---|
| `Models/` | `Card` (abstract), `DisasterCard`, `ResponseCard`, `SpecificCard`, `GeneralCard`, `MemeCard`, enums | Member 1 |
| `Data/CardData.cs` | 6 disasters, 30 response cards (24 specific + 6 general), 12 Meme cards | Member 1 |
| `Engine/IGameManager.cs` | Integration contract. Change only with approval from all 4 | Everyone |
| `Engine/Deck.cs`, `Engine/Player.cs`, `Engine/GameManager.cs` | Real game engine. Has `TODO (Member 2)` steps | Member 2 |
| `Engine/FakeGameManager.cs` | Temporary engine for testing the UI. Delete when `GameManager` works | Member 3 |
| `UI/Controls/CardPanel.cs` | Clickable card drawn with GDI+ | Member 3 |
| `UI/Controls/PixelPanel.cs` | One comic panel, nearest-neighbor pixel art | Member 3 |
| `UI/Forms/GameForm.cs` | Gameplay screen (3 panels, hearts, status, scenario, hand, message) | Member 3 |
| `UI/Theme.cs`, `UI/AssetLoader.cs` | Colors, fonts and image loading | Member 3 |
| `UI/Forms/MenuForm.cs`, `GameOverForm.cs`, `HowToPlayForm.cs` | Menu, Game Over and rules screens | Member 4 |
| `Services/ScoreManager.cs` | Save/load high score. Has `TODO (Member 4)` steps | Member 4 |
| `Assets/*.png` | 12 pixel art images (64 x 64). Current ones are PLACEHOLDERS | Member 4 |

Search the solution for `TODO` (Ctrl+Shift+F) to see each member's remaining work.

## Replacing the pixel art

Keep the same file names and size (64 x 64 PNG) and just overwrite the files in `Assets/`:

- Panel 1: `scene_flood.png`, `scene_fire.png`, `scene_typhoon.png`, `scene_earthquake.png`, `scene_landslide.png`, `scene_drought.png`
- Panel 2: `face_calm.png` (Perfect), `face_panic.png` (before the pick, Partial, Wrong), `face_meme.png` (Meme card)
- Panel 3: `result_safe.png` (Perfect), `result_hurt.png` (Partial), `result_fail.png` (Wrong or Meme)

If a file is missing, the panel shows a dashed placeholder instead of crashing.

## GitHub rules

- Never code directly on `main`. Use one branch per deliverable, for example `feature/B3-deck-player`.
- Open a Pull Request when done. A teammate reviews it before merging.
- Only the owner of a file edits it. Problems in someone else's file go to the group chat.
- Start each session with `git pull origin main`. Never commit `bin/` or `obj/` (the `.gitignore` already blocks them).
