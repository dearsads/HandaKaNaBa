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
| `UIManager.cs` | 3 | Menu, HUD, encounter panel with the 10-second timer, end screen |
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
