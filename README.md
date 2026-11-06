# Handa Ka Na Ba?

IT111L Concepts in OOP machine problem. C# Windows Forms (.NET 8, Visual Studio 2022).

Walk a 12x12 barangay, dodge the disaster, rescue Lola, Bunso and Bantay through card encounters, then run to the evacuation center.

## Run

Open `HandaKaNaBa.sln` and press F5.
Controls: WASD or arrow keys to move. During a card, click or press 1, 2, 3. Enter or Space to continue.

## Files

| File | Member | Contents |
| --- | --- | --- |
| `EncounterDatabase.cs` | 1 | `Card` (abstract), `HandaCard`, `PwedeNaCard`, `SablayCard`, `Scenario`, 9 scenarios, 3 maps, report |
| `GameEngine.cs` | 2 | `GridEntity` (abstract), `Player`, `FamilyMember`, hearts, score, combo, collisions, stars |
| `DisasterManager.cs` | 2 | `Hazard` (abstract), `Tsunami`, `Lindol`, `ElNino`, the 12x12 map |
| `UIManager.cs` | 3 | Menu, HUD, encounter panel with the 15-second timer, end screen |
| `MainForm.cs` | 4 | Program entry, game loop, keyboard, grid drawing, sprite loading |
| `Assets/` | 4 | 12 sprites (16x16 PNG): floor, grass, wall, high, evac, water, crack, dry, player, lola, bunso, bantay |

## Disasters

- **Tsunami (Easy):** a 3-column wave sweeps from the left. Stand on the green hills. Waves get faster after the 3rd.
- **Lindol (Medium):** cracks in rows and columns flash red, then hurt. They come faster and get longer over time.
- **El Niño (Hard):** 3x3 patches of grass turn brown, then dry and cracked. Standing on dry ground hurts. More patches at once every 30 seconds, mostly near you.

## Scoring

- HANDA: rescued, +100 x combo (max x3), +10 per second left. Every 3rd HANDA in a row: +1 heart.
- PWEDE NA: rescued, +40, combo resets.
- SABLAY or time out: -1 heart, pushed back one tile, try again.
- Reaching EVAC: +200, +5 per second under 90 s, +300 if never hit.
- Stars: all HANDA, never hit, finished within 90 s.

## Changes on the `finalization` branch

- Refactored `GameEngine.ChooseCard` to route each answer result to a clearly named method: `ApplyHandaResult`, `ApplyPwedeNaResult`, or `ApplySablayResult`. This makes each outcome easier to read while keeping its existing game rules.
- Changed each scenario card's answer timer from 10 to 15 seconds. A prominent countdown with an `s` seconds label appears in the upper-left of the encounter screen and resets to `15s` whenever a new scenario opens.
- Added same-line comments in the runnable `HandaKaNaBa/` source to explain beginner OOP examples such as inheritance, abstraction, encapsulation, and polymorphism, along with C# basics like enums, lists, dictionaries, events, timers, lambdas, arrays, and drawing.
- Added this guide, the end-to-end game flow, two-hour study schedule, and a short presentation opening.

## Beginner's code guide

Start with the runnable project in `HandaKaNaBa/`. The separate `Reference 2000+lines/` folder is not needed to understand or run this version.

### What each file does

| File | In plain language |
| --- | --- |
| `MainForm.cs` | Starts the window, reads movement keys, runs the regular game update, and draws the map and sprites. |
| `UIManager.cs` | Builds the menu, status bar, rescue-card screen, and ending screen. |
| `GameEngine.cs` | Owns the game rules: movement, lives, score, rescuing people, and winning or losing. |
| `DisasterManager.cs` | Builds the map and updates the chosen disaster. |
| `EncounterDatabase.cs` | Stores the scenarios, answer cards, maps, and end-of-game report. |

### OOP ideas you can point to

- **Class and object:** `Player` is the kind of thing; `engine.Player` is the player object used in the current game.
- **Encapsulation:** `GameEngine` keeps `lives` and `score` private, then exposes read-only `Lives` and `Score` properties so other classes can display them without changing them directly.
- **Inheritance:** `Player` and `FamilyMember` share position and sprite behavior from `GridEntity`. `Tsunami`, `Lindol`, and `ElNino` share a common parent, `Hazard`.
- **Abstraction:** `GridEntity`, `Card`, and `Hazard` describe what their child classes must provide without deciding every detail themselves.
- **Polymorphism:** the game can ask the current `Hazard` whether a tile is dangerous; the selected disaster supplies its own answer. Card subclasses also provide their own point calculation.
- **Composition:** `GameEngine` owns a `DisasterManager` and an `EncounterDatabase`; it uses these parts to run a level.

### Other C# and Windows Forms concepts in this app

These are tools the project uses alongside OOP; you do not need to claim they are OOP concepts.

- **Enum:** a small named set of choices. `GameState` lists the possible game screens/states, and `Difficulty` lists the disasters.
- **Array, `List`, and `Dictionary`:** ways to keep groups of data. The map is a 2D array, `Family` is a `List<FamilyMember>`, and `FamilySpots` is a `Dictionary` that pairs a family type with a map position.
- **Random:** chooses a hazard pattern and shuffles the answer cards so the correct answer is not always in the same position.
- **Timer:** a Windows Forms timer repeatedly calls code. `MainForm` updates the game about every 100 milliseconds; `UIManager` counts down the answer time once per second.
- **Event:** a notification that something happened. For example, `GameEngine` raises `EncounterStarted`, and `UIManager` responds by showing the rescue-card screen.
- **Lambda / callback:** a short function supplied to run later. In `UIManager`, button click handlers use `(s, e) => ...` to say what should happen when a player clicks.
- **LINQ:** shorthand operations for checking or finding items in a collection. `UIManager` uses `.Any(...)` to check whether the optional pixel font is installed.
- **`using` block:** makes sure a drawing resource such as a brush is released when its work is done.
- **Drawing (`Graphics`):** Windows Forms gives `MainForm.OnPaint` a drawing surface. The method paints map tiles, hazards, family members, and the player; it does not decide the game rules.

### Follow one complete game flow

1. `Program.Main` opens `MainForm`; the form creates a `GameEngine` and a `UIManager`.
2. Choosing a disaster in the menu calls `GameEngine.StartLevel`, which loads that disaster's map and creates the player and family members.
3. Pressing a movement key is read by `MainForm.ProcessCmdKey`, which asks the engine to move the player.
4. The engine checks the new tile. It may start a rescue encounter, lose a life, or let the player reach the exit after everyone is rescued.
5. When an encounter starts, the engine raises `EncounterStarted`. The UI manager hears that event and displays the scenario and three cards.
6. Choosing a card calls `GameEngine.ChooseCard`. HANDA rescues and scores; PWEDE NA rescues with fewer points; SABLAY costs a life and returns the player to the previous tile. The UI then displays the reaction and lesson.
7. The main timer calls `GameEngine.Tick` while walking, so the disaster advances. When the game ends, the engine raises `Ended` and the UI manager displays the result.

In `GameEngine.ChooseCard`, read the selected result first, then follow the matching `ApplyHandaResult`, `ApplyPwedeNaResult`, or `ApplySablayResult` method. Those methods separate the three outcomes so each one is easier to explain.

### Two-hour beginner study plan

Use the code as a map; do not try to memorize every line or every Windows Forms drawing call.

| Time | What to do |
| --- | --- |
| 0–10 min | Run the game. Explain its goal, controls, rescues, disasters, and exit in your own words. |
| 10–30 min | Find `GridEntity`, `Player`, `FamilyMember`, `Card`, and `Hazard`. Explain inheritance, abstraction, encapsulation, composition, and polymorphism with one example each. |
| 30–50 min | Trace `MovePlayer`, `CheckHazard`, `ChooseCard`, and `EndLevel` in `GameEngine.cs`. Explain what changes after a move, a hit, each answer, and a win. |
| 50–70 min | Follow a key press and a rescue card through `MainForm.cs` and `UIManager.cs`. Explain events as notifications and timers as repeated updates. |
| 70–90 min | Look at `DisasterManager.cs` and `EncounterDatabase.cs`. Explain how one map, hazard, and scenario are chosen and updated. |
| 90–105 min | Close the files and draw the path from `Program.Main` to the ending screen. Reopen files only to correct missing steps. |
| 105–115 min | Practice answering: “Where are the OOP examples?”, “What happens when I choose SABLAY?”, and “How does the screen know to show an encounter?” |
| 115–120 min | Give a short demo and explain which parts contain game rules and which parts draw or display the game. |

### A short presentation opening

> Handa Ka Na Ba is a C# Windows Forms game about preparing for disasters. The player walks around a map, avoids hazards, rescues three family members by choosing safety answers, and then reaches the evacuation center. The OOP structure separates the rules, disaster behavior, scenario data, and screen display so each part has a clear job.
