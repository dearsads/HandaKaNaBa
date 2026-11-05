// =====================================================================
//  GameEngine.cs
//  OWNER: Member 2 - Back-End (Grid Mechanics)
//
//  The brain: player position, hearts, score, combo, game state,
//  collisions (hazard / family / Go-Bag / exit) and the card results.
//  It never draws anything. It tells the UI what happened through EVENTS.
//
//  OOP:
//    - GridEntity is ABSTRACT; Player, FamilyMember and Pickup INHERIT it.
//    - ENCAPSULATION: lives and score are private. Other files can only
//      read them; they change through this class's own methods.
//    - COMPOSITION: GameEngine owns the DisasterManager, the
//      EncounterDatabase, the Player and the family.
// =====================================================================
namespace HandaKaNaBa
{
    public enum GameState { Menu, Walking, Encounter, LevelClear, GameOver }

    // ------------------------------------------------------------------
    //  ENTITIES ON THE GRID
    // ------------------------------------------------------------------
    public abstract class GridEntity
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }

        protected GridEntity(int x, int y)
        {
            X = x;
            Y = y;
        }

        public abstract string Sprite { get; }               // file name in Assets (without .gif/.png)
        public Point Position => new Point(X, Y);
        public bool IsAt(int x, int y) => X == x && Y == y;
        public bool IsAt(Point p) => X == p.X && Y == p.Y;
    }

    public class Player : GridEntity
    {
        public int InvincibleTicks { get; private set; }
        public bool HasShield { get; private set; }

        public Player(int x, int y) : base(x, y) { }

        public override string Sprite => "player";
        public bool IsInvincible => InvincibleTicks > 0;

        public void MoveTo(int x, int y) { X = x; Y = y; }
        public void MakeInvincible(int ticks) => InvincibleTicks = Math.Max(InvincibleTicks, ticks);
        public void GiveShield() => HasShield = true;
        public void UseShield() => HasShield = false;
        public void TickTimers() { if (InvincibleTicks > 0) InvincibleTicks--; }
    }

    public class FamilyMember : GridEntity
    {
        /// <summary>Hits a family member can take before they are lost. Set to 1 for "one hit and gone".</summary>
        public const int MaxSafety = 2;

        private int hitCooldown;

        public FamilyType Type { get; }
        public int Safety { get; private set; } = MaxSafety;
        public bool IsRescued { get; private set; }
        public bool IsLost { get; private set; }
        public bool IsWarned { get; set; }                   // a hazard is flashing on their tile
        public int Attempts { get; private set; }            // cards played to rescue them

        public FamilyMember(FamilyType type, int x, int y) : base(x, y)
        {
            Type = type;
        }

        public override string Sprite => Type.ToString().ToLower();   // lola, bunso, bantay
        public string Name => EncounterDatabase.NameOf(Type);
        public bool NeedsRescue => !IsRescued && !IsLost;

        public void Rescue() => IsRescued = true;
        public void AddAttempt() => Attempts++;
        public void FollowTo(Point p) { X = p.X; Y = p.Y; }
        public void TickTimers() { if (hitCooldown > 0) hitCooldown--; }

        /// <summary>Returns true if the hit counted (there is a short cooldown between hits).</summary>
        public bool TakeHit()
        {
            if (hitCooldown > 0 || !NeedsRescue) return false;
            Safety--;
            hitCooldown = 20;
            if (Safety <= 0) IsLost = true;
            return true;
        }
    }

    public class Pickup : GridEntity
    {
        public bool Taken { get; private set; }

        public Pickup(int x, int y) : base(x, y) { }

        public override string Sprite => "gobag";
        public void Take() => Taken = true;
    }

    // ------------------------------------------------------------------
    //  THE ENGINE
    // ------------------------------------------------------------------
    public class GameEngine
    {
        // ---- Tuning knobs (change these freely while playtesting) ----
        public const int MaxLives = 5;
        public const int TicksPerSecond = 10;
        public const int ParSeconds = 90;          // finish faster than this for a time bonus
        public const int HitInvincibleTicks = 15;  // 1.5 s of blinking after a hit

        private static readonly Dictionary<Difficulty, int> bestScores = new Dictionary<Difficulty, int>();

        private int lives = MaxLives;
        private int score;
        private int combo;
        private bool answered;
        private bool clutch;
        private readonly List<Point> trail = new List<Point>();   // player's previous tiles, newest first

        // ---- Read-only state for the UI and the renderer ----
        public GameState State { get; private set; } = GameState.Menu;
        public Difficulty Disaster { get; private set; }
        public LevelData Level { get; private set; }
        public int Lives => lives;
        public int Score => score;
        public int Combo => combo;
        public int Multiplier => Math.Max(1, Math.Min(combo, 3));
        public int LevelTicks { get; private set; }
        public int Seconds => LevelTicks / TicksPerSecond;

        public DisasterManager Map { get; } = new DisasterManager();
        public EncounterDatabase Database { get; } = new EncounterDatabase();
        public Player Player { get; private set; }
        public List<FamilyMember> Family { get; } = new List<FamilyMember>();
        public Pickup GoBag { get; private set; }

        public bool ExitOpen => Family.Count > 0 && Family.All(f => !f.NeedsRescue);
        public int RescuedCount => Family.Count(f => f.IsRescued);
        public int LostCount => Family.Count(f => f.IsLost);

        // ---- Encounter ----
        public FamilyMember CurrentTarget { get; private set; }
        public Scenario CurrentScenario { get; private set; }
        public Card LastCard { get; private set; }               // null = the panic timer ran out
        public CardResult LastResult { get; private set; }
        public string LastScoreText { get; private set; } = "";

        // ---- End of level ----
        public int HazardHits { get; private set; }
        public int FirstTryHanda { get; private set; }
        public int Stars { get; private set; }
        public bool NewBest { get; private set; }
        public List<string> Breakdown { get; } = new List<string>();

        public static int BestScore(Difficulty d) => bestScores.TryGetValue(d, out int s) ? s : 0;

        // ---- Events (UIManager listens to these) ----
        public event Action LivesChanged;
        public event Action EncounterStarted;
        public event Action LevelEnded;                    // cleared or game over: check State
        public event Action<string, Color> Popup;          // short floating message

        // ===============================================================
        //  STARTING
        // ===============================================================
        public void StartLevel(Difficulty disaster)
        {
            Disaster = disaster;
            Level = EncounterDatabase.GetLevel(disaster);
            Map.Load(Level);
            Map.ImportantPoints = ImportantPoints;

            lives = MaxLives;
            score = 0;
            combo = 0;
            answered = false;
            LevelTicks = 0;
            HazardHits = 0;
            FirstTryHanda = 0;
            Stars = 0;
            NewBest = false;
            Breakdown.Clear();
            trail.Clear();
            Database.ResetRun();

            Player = new Player(Map.Start.X, Map.Start.Y);
            Family.Clear();
            foreach (KeyValuePair<FamilyType, Point> start in Map.FamilyStarts)
                Family.Add(new FamilyMember(start.Key, start.Value.X, start.Value.Y));
            GoBag = Map.GoBag.X >= 0 ? new Pickup(Map.GoBag.X, Map.GoBag.Y) : null;

            CurrentTarget = null;
            CurrentScenario = null;
            State = GameState.Walking;

            LivesChanged?.Invoke();
            Popup?.Invoke(Level.Title, Color.White);
        }

        public void ReturnToMenu() => State = GameState.Menu;

        private IEnumerable<Point> ImportantPoints()
        {
            yield return Player.Position;
            yield return Map.Exit;
            foreach (FamilyMember f in Family)
                if (!f.IsLost) yield return f.Position;
            if (GoBag != null && !GoBag.Taken) yield return GoBag.Position;
        }

        // ===============================================================
        //  MOVEMENT (called by MainForm on WASD / arrow keys)
        // ===============================================================
        public void MovePlayer(int dx, int dy)
        {
            if (State != GameState.Walking) return;

            int nx = Player.X + dx;
            int ny = Player.Y + dy;
            if (!Map.IsWalkable(nx, ny)) return;

            trail.Insert(0, Player.Position);
            if (trail.Count > 8) trail.RemoveAt(trail.Count - 1);
            Player.MoveTo(nx, ny);
            UpdateFollowers();

            CheckGoBag();
            CheckHazardOnPlayer();
            if (State != GameState.Walking) return;
            CheckFamily();
            if (State != GameState.Walking) return;
            CheckExit();
        }

        // Rescued family members walk behind the player like a snake.
        private void UpdateFollowers()
        {
            int i = 0;
            foreach (FamilyMember f in Family.Where(m => m.IsRescued))
            {
                if (i < trail.Count) f.FollowTo(trail[i]);
                i++;
            }
        }

        // ===============================================================
        //  GAME LOOP (called every 100 ms by MainForm)
        // ===============================================================
        public void Tick()
        {
            if (State != GameState.Walking) return;

            LevelTicks++;
            Player.TickTimers();
            Map.Tick(Player.X, Player.Y);

            CheckHazardOnPlayer();
            if (State != GameState.Walking) return;
            CheckHazardOnFamily();
        }

        // ===============================================================
        //  COLLISIONS
        // ===============================================================
        private void CheckHazardOnPlayer()
        {
            if (Player.IsInvincible || !Map.IsDanger(Player.X, Player.Y)) return;

            if (Player.HasShield)
            {
                Player.UseShield();
                Player.MakeInvincible(HitInvincibleTicks);
                Popup?.Invoke("GO-BAG SHIELD! Ligtas ka.", UIManager.Cyan);
                return;
            }

            HazardHits++;
            Player.MakeInvincible(HitInvincibleTicks);
            LoseLife();
            Popup?.Invoke("ARAY! -1 PUSO", UIManager.Pink);
        }

        private void CheckHazardOnFamily()
        {
            foreach (FamilyMember f in Family.Where(m => m.NeedsRescue))
            {
                f.TickTimers();
                f.IsWarned = Map.IsWarning(f.X, f.Y) || Map.IsDanger(f.X, f.Y);
                if (!Map.IsDanger(f.X, f.Y) || !f.TakeHit()) continue;

                if (f.IsLost)
                {
                    Popup?.Invoke("NAWALA SI " + f.Name.ToUpper() + "!", UIManager.Pink);
                    Database.RecordMiss(EncounterDatabase.LostLesson);
                    if (ExitOpen) Popup?.Invoke("BUKAS NA ANG EVACUATION CENTER!", UIManager.Green);
                }
                else
                {
                    Popup?.Invoke("NATAMAAN SI " + f.Name.ToUpper() + "! BILISAN!", UIManager.Orange);
                }
            }
        }

        private void CheckGoBag()
        {
            if (GoBag == null || GoBag.Taken || !Player.IsAt(GoBag.Position)) return;
            GoBag.Take();
            Player.GiveShield();
            AddScore(50);
            Popup?.Invoke("GO-BAG! May shield ka na. +50", UIManager.Cyan);
        }

        private void CheckFamily()
        {
            FamilyMember target = Family.FirstOrDefault(f => f.NeedsRescue && f.IsAt(Player.Position));
            if (target != null) StartEncounter(target);
        }

        private void CheckExit()
        {
            if (Player.IsAt(Map.Exit))
            {
                if (ExitOpen) EndLevel(true);
                else Popup?.Invoke("SARADO PA! Sagipin muna ang pamilya.", UIManager.Yellow);
            }
        }

        private void LoseLife()
        {
            lives = Math.Max(0, lives - 1);
            combo = 0;
            LivesChanged?.Invoke();
            if (lives <= 0 && State == GameState.Walking) EndLevel(false);
        }

        private void AddScore(int points)
        {
            if (points > 0) score += points;
        }

        // ===============================================================
        //  ENCOUNTERS (the card phase)
        // ===============================================================
        private void StartEncounter(FamilyMember target)
        {
            CurrentTarget = target;
            clutch = target.IsWarned;                      // rescued while a hazard was flashing on them
            CurrentScenario = Database.Draw(Disaster, target.Type);
            LastCard = null;
            LastScoreText = "";
            answered = false;
            State = GameState.Encounter;
            EncounterStarted?.Invoke();
        }

        /// <summary>
        /// Called by UIManager when a card is clicked (index 0-2) or the timer runs out (index -1).
        /// The game stays paused until ResumeAfterEncounter() is called.
        /// </summary>
        public CardResult ChooseCard(int index, int secondsLeft)
        {
            if (State != GameState.Encounter || CurrentScenario == null || answered) return LastResult;
            answered = true;

            CardResult result = Database.CheckAnswer(CurrentScenario, index);
            LastCard = index >= 0 && index < CurrentScenario.Cards.Length ? CurrentScenario.Cards[index] : null;
            LastResult = result;
            FamilyMember target = CurrentTarget;
            target.AddAttempt();

            var parts = new List<string>();
            switch (result)
            {
                case CardResult.Handa:
                {
                    combo++;
                    int points = LastCard.GetPoints(Multiplier, secondsLeft);
                    AddScore(points);
                    parts.Add("+" + points);
                    if (combo >= 2) parts.Add("COMBO x" + Multiplier);
                    if (target.Attempts == 1) FirstTryHanda++;
                    if (clutch)
                    {
                        AddScore(50);
                        parts.Add("CLUTCH SAVE +50");
                    }
                    if (combo % 3 == 0)
                    {
                        if (lives < MaxLives)
                        {
                            lives++;
                            LivesChanged?.Invoke();
                            parts.Add("BAYANIHAN BONUS +1 PUSO");
                        }
                        else
                        {
                            AddScore(150);
                            parts.Add("BAYANIHAN BONUS +150");
                        }
                    }
                    target.Rescue();
                    break;
                }

                case CardResult.PwedeNa:
                {
                    combo = 0;
                    int points = LastCard.GetPoints(1, secondsLeft);
                    AddScore(points);
                    parts.Add("+" + points + "  (combo reset)");
                    Database.RecordMiss(CurrentScenario.BestCard.Lesson);
                    target.Rescue();
                    break;
                }

                default:   // Sablay or timeout: lose a heart, get pushed back, try again later
                {
                    combo = 0;
                    lives = Math.Max(0, lives - 1);
                    LivesChanged?.Invoke();
                    parts.Add("-1 PUSO  (subukan ulit)");
                    Database.RecordMiss(CurrentScenario.BestCard.Lesson);
                    if (trail.Count > 0)
                    {
                        Player.MoveTo(trail[0].X, trail[0].Y);
                        trail.RemoveAt(0);
                    }
                    break;
                }
            }

            LastScoreText = string.Join("   ", parts);
            return result;
        }

        public void ResumeAfterEncounter()
        {
            if (State != GameState.Encounter) return;

            FamilyMember target = CurrentTarget;
            CurrentScenario = null;
            CurrentTarget = null;
            answered = false;

            if (lives <= 0)
            {
                EndLevel(false);
                return;
            }

            State = GameState.Walking;
            Player.MakeInvincible(10);       // a short grace period after reading
            UpdateFollowers();

            if (target != null && target.IsRescued)
            {
                if (ExitOpen) Popup?.Invoke("BUKAS NA ANG EVACUATION CENTER!", UIManager.Green);
                else Popup?.Invoke(RescuedCount + "/" + Family.Count + " NASAGIP!", UIManager.Green);
            }
        }

        // ===============================================================
        //  END OF LEVEL
        // ===============================================================
        private void EndLevel(bool cleared)
        {
            State = cleared ? GameState.LevelClear : GameState.GameOver;
            Breakdown.Clear();
            Stars = 0;

            if (cleared)
            {
                AddScore(200);
                Breakdown.Add("Nakarating sa evacuation center: +200");

                int timeBonus = Math.Max(0, ParSeconds - Seconds) * 5;
                if (timeBonus > 0)
                {
                    AddScore(timeBonus);
                    Breakdown.Add("Bilis (" + Seconds + "s): +" + timeBonus);
                }
                if (HazardHits == 0)
                {
                    AddScore(300);
                    Breakdown.Add("WALANG GALOS: +300");
                }

                bool noneLost = LostCount == 0;
                bool allFirstTry = FirstTryHanda == Family.Count;
                bool noHits = HazardHits == 0;
                if (noneLost) Stars++;
                if (allFirstTry) Stars++;
                if (noHits) Stars++;
                Breakdown.Add((noneLost ? "★" : "☆") + " Walang naiwan sa pamilya");
                Breakdown.Add((allFirstTry ? "★" : "☆") + " Puro HANDA sa unang subok");
                Breakdown.Add((noHits ? "★" : "☆") + " Walang tama ng sakuna");

                if (score > BestScore(Disaster))
                {
                    bestScores[Disaster] = score;
                    NewBest = true;
                }
            }

            LevelEnded?.Invoke();
        }
    }
}
