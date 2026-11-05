# Handa Ka Na Ba? (Survival Grid)

IT111L Concepts in OOP machine problem. C# Windows Forms, .NET 8, Visual Studio 2022.

Walk a 12x12 barangay, dodge the disaster, rescue Lola, Bunso and Bantay with card encounters, then reach the evacuation center.

## Run it

1. Open `HandaKaNaBa.sln` in Visual Studio 2022 (".NET desktop development" workload).
2. Press F5.

Controls: WASD or arrow keys to move. During a card: click, or press 1 / 2 / 3. Enter or Space to continue.

## The 5 files

| File | Owner | What is inside |
| --- | --- | --- |
| `EncounterDatabase.cs` | Member 1, Data & Scenarios | `Card` (abstract) + `HandaCard`, `PwedeNaCard`, `SablayCard`; `Scenario`; the 12 scenarios; the 3 maps; the Barangay Assembly Report |
| `GameEngine.cs` | Member 2, Grid Mechanics | `GridEntity` (abstract) + `Player`, `FamilyMember`, `Pickup`; hearts, score, combo, collisions, card results, stars |
| `DisasterManager.cs` | Member 2, Grid Mechanics | `Hazard` (abstract) + `TsunamiHazard`, `EarthquakeHazard`, `VolcanoHazard`; the 12x12 terrain; permanent fissures |
| `UIManager.cs` | Member 3, UI & Timers | Menu, HUD, encounter panel with the panic timer, end screen, popups and banners |
| `MainForm.cs` | Member 4, Assets & Renderer | `Program.Main`, `Sprites` (asset loader), game loop, keyboard, `OnPaint` grid drawing |

There are no `.Designer.cs` files. Every panel is built in code, so merging on GitHub never conflicts on designer files.

## How the game plays

- **Difficulty select:** Tsunami (Easy), Lindol (Medium), Bulkan (Hard).
- **Tsunami:** a 3-column wave sweeps from the left. Stand on green high ground (`^`). Waves come faster over time.
- **Lindol:** cracks flash, then hurt. The longer you take, the faster and longer they get. Some cracks stay forever as fissures (walls). A fissure never traps you or cuts off a family member, and the map holds at most 30 fissure tiles. An aftershock every 25 s spawns 3 cracks at once.
- **Bulkan:** 3x3 blasts with a warning shadow, mostly near you. More blasts at once every 30 s.
- **Family can be lost:** each family member can take 2 hazard hits (green pips). A "!" bubble means a hazard is flashing on them. At 0 they are gone for good, and you lose a star.
- **Card encounter:** step on a family member to get 3 vague cards and a 10-second panic timer.
  - **HANDA:** they are rescued. +100 x combo, plus 10 points per second left.
  - **PWEDE NA:** they are rescued. +40, and the combo resets.
  - **SABLAY or time out:** -1 heart, and you are pushed back. Try again with a new scenario.
  - After you pick, all 3 cards flip to show why each one was right or wrong.
- **Rewards:**
  - Bayanihan Bonus: 3 HANDA in a row gives +1 heart (or +150 if full).
  - Clutch Save: rescuing someone while a hazard is flashing on them gives +50.
  - Go-Bag: a shield that blocks one hit.
  - Rescued family follow behind you.
- **Clear:** reach EVAC after everyone is rescued or lost. You get +200, a time bonus and +300 for no hits, plus up to 3 stars:
  - nobody lost
  - all HANDA on the first try
  - no hazard hits
- **Barangay Assembly Report:** your rank, plus the lesson from every card you got wrong.

## Tuning knobs (change while playtesting)

| What | Where |
| --- | --- |
| Hearts | `GameEngine.MaxLives` (5) |
| Panic timer | `UIManager.PanicSeconds` (10) |
| Hits a family member can take | `FamilyMember.MaxSafety` (2; set to 1 for "one hit and gone") |
| Time bonus par | `GameEngine.ParSeconds` (90) |
| Fissure cap | `DisasterManager.MaxFissures` (30) |
| Earthquake speed-up | `EarthquakeHazard.SpawnEvery`, `CrackLength`, `PermanentChance` |
| Tsunami speed-up | `TsunamiHazard.CalmTicks`, `StepTicks` |
| Volcano speed-up | `VolcanoHazard.SpawnEvery`, `BlastsPerWave` |
| Maps | `EncounterDatabase.GetLevel` (12 strings of 12 characters) |

## Assets (Member 4)

Every file in `Assets/` is a temporary placeholder. Replace it with your own art using the same name. Draw at **16x16**. The game scales it 3x to 48x48 with nearest-neighbor, so it stays crisp.

- **Animated:** save as `.gif`, and it plays automatically. The game looks for `.gif` first, then `.png`.
- **Transparent background:** characters, Go-Bag, hearts and faces.
- **Full tile, no transparency:** floor, wall, high, evac, fissure, water, crack, lava.

| Name | Used for |
| --- | --- |
| `floor`, `wall`, `high`, `evac`, `fissure` | Terrain |
| `water`, `crack`, `lava` | Danger tiles (Tsunami, Lindol, Bulkan) |
| `player`, `lola`, `bunso`, `bantay`, `gobag` | Characters and pickup |
| `heart_full`, `heart_empty` | HUD |
| `face_alert`, `face_handa`, `face_pwede`, `face_sablay` | Meme face in the encounter panel |

A missing file never crashes the game: it draws a colored square with a letter instead.

Optional: drop `Bangers-Regular.ttf` (free on Google Fonts) into `Assets/` for the comic title font. Without it, the game uses Impact.

## GitHub rules

- One branch per member, for example `member2-hazards`. Open a Pull Request and Member 4 merges.
- Only edit your own file. If you need something from another file, ask its owner.
- Never commit `bin/` or `obj/`. The `.gitignore` already blocks them.
