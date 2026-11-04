# Handa Ka Na Ba?

Disaster survival card game for IT111L Concepts in OOP (Laboratory) - Machine Problem.
C# Windows Forms, .NET 8, Visual Studio 2022.

## Running the game

1. Open `HandaKaNaBa.sln` in Visual Studio 2022 (with the ".NET desktop development" workload).
2. In Solution Explorer, right-click **HandaKaNaBa** > **Set as Startup Project**.
3. Press F5.

## Running the console test

1. Right-click **HandaKaNaBa.ConsoleTest** > **Set as Startup Project**.
2. Press F5. Every line should say `PASS`, and the last line should say `ALL TESTS PASSED`.
3. It also prints a sample game (5 rounds) in the same format as the Expected Output in the doc.

## Team and files

### Development Team - Back End & Game Engine (Members 1 & 2)

| File | What it does |
|---|---|
| `Models/Card.cs` | Abstract base class: Name, Emoji, Description (Abstraction) |
| `Models/DisasterCard.cs` | Disaster type, severity and scenario text (Inheritance) |
| `Models/ResponseCard.cs` | Base class with `virtual Evaluate()` (Polymorphism) |
| `Models/SpecificCard.cs` | Perfect for its own disaster, Wrong for the rest |
| `Models/GeneralCard.cs` | Always Partial |
| `Models/MemeCard.cs` | Always Wrong, carries a funny line and the lesson |
| `Data/CardData.cs` | 6 disasters, 30 response cards (24 specific + 6 general), 12 Meme cards |
| `Engine/Deck.cs` | Shuffle, draw, discard, reshuffle the discard pile when empty |
| `Engine/Player.cs` | Private lives, score, streak and hand (Encapsulation) |
| `Engine/GameManager.cs` | Rounds, levels, damage table, scoring, fairness rule, Meme rule, win/lose (Composition) |
| `Engine/IGameManager.cs` | Integration contract between the engine and the UI |
| `HandaKaNaBa.ConsoleTest/Program.cs` | Console test of all of the above |

### Front End & UI Implementation (Members 2 & 1)

| File | What it does |
|---|---|
| `UI/Forms/GameForm.cs` | Gameplay screen: 3 comic panels, hearts, status, scenario, hand, message, Next button |
| `UI/Controls/CardPanel.cs` | Clickable card drawn with GDI+ (hover effect, disabled state) |
| `UI/Controls/PixelPanel.cs` | One comic panel; draws pixel art with nearest-neighbor scaling |
| `UI/Forms/MenuForm.cs` | Play, How to Play, High Score, Exit |
| `UI/Forms/GameOverForm.cs` | Win/lose, final score, level reached, Play Again / Main Menu |
| `UI/Forms/HowToPlayForm.cs` | Rules screen |
| `Services/ScoreManager.cs` | Saves and loads the high score (`highscore.txt` next to the .exe) |
| `UI/Theme.cs`, `UI/AssetLoader.cs` | Colors, fonts and image loading |
| `Assets/*.png` | 12 pixel art images (64 x 64). Current ones are placeholders |

## Replacing the pixel art

Keep the same file names and size (64 x 64 PNG) and overwrite the files in `Assets/`:

- Panel 1: `scene_flood.png`, `scene_fire.png`, `scene_typhoon.png`, `scene_earthquake.png`, `scene_landslide.png`, `scene_drought.png`
- Panel 2: `face_calm.png` (Perfect), `face_panic.png` (before the pick, Partial, Wrong), `face_meme.png` (Meme card)
- Panel 3: `result_safe.png` (Perfect), `result_hurt.png` (Partial), `result_fail.png` (Wrong or Meme)

If a file is missing, the panel shows a dashed placeholder instead of crashing.

## GitHub rules

- Never code directly on `main`. Use one branch per change, for example `feature/full-implementation`.
- Open a Pull Request when done. A teammate reviews it before merging.
- Start each session with `git pull origin main`. Never commit `bin/` or `obj/` (the `.gitignore` already blocks them).
