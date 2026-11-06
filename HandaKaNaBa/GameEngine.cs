// GameEngine.cs - Member 2 (Grid Mechanics)
// Ang "utak" ng laro: player, puso, score, combo, collision at resulta ng cards.
// Hindi ito nagdo-drawing. Sinasabihan lang nito ang UI gamit ang EVENTS.
namespace HandaKaNaBa
{
    public enum GameState { Menu, Walking, Encounter, Clear, GameOver } // Enum: fixed named choices for the game's current state.

    // ABSTRACT parent ng lahat ng nasa grid
    public abstract class GridEntity // Abstraction: shared idea for anything with a position on the map.
    {
        public int X { get; protected set; } // A protected setter lets this class and child classes change the position.
        public int Y { get; protected set; }

        protected GridEntity(int x, int y) // Constructor: runs when a new grid entity is created.
        {
            X = x;
            Y = y;
        }

        public abstract string Sprite { get; } // Child classes must provide their own sprite name.
        public bool IsAt(int x, int y) => X == x && Y == y;
    }

    public class Player : GridEntity // Inheritance: Player reuses position and behavior from GridEntity.
    {
        public int Invincible { get; set; }   // ticks na hindi tatamaan pagkatapos masaktan

        public Player(int x, int y) : base(x, y) { } // base(...) calls the parent constructor to set the starting position.

        public override string Sprite => "player"; // override supplies this child's version of the parent's abstract property.

        public void MoveTo(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class FamilyMember : GridEntity // Another child class; it shares the same position rules as Player.
    {
        public FamilyType Type { get; }
        public bool Rescued { get; set; }

        public FamilyMember(FamilyType type, int x, int y) : base(x, y)
        {
            Type = type;
        }

        public override string Sprite => Type.ToString().ToLower();   // override: choose the sprite name from this member's type.
        public string Name => Type.ToString();
    }

    public class GameEngine
    {
        public const int MaxLives = 5;

        // ENCAPSULATION: private, nababasa lang ng ibang class pero hindi nababago
        private int lives;
        private int score;
        private int combo;
        private int prevX, prevY;   // huling tile bago gumalaw (pang push-back)

        public GameState State { get; private set; } = GameState.Menu; // Other classes can read State, but only GameEngine can change it.
        public Difficulty Disaster { get; private set; }
        public int Lives => lives;
        public int Score => score;
        public int Multiplier => Math.Max(1, Math.Min(combo, 3));
        public int Ticks { get; private set; }
        public int Seconds => Ticks / 10;
        public int Hits { get; private set; }
        public int Stars { get; private set; }
        public List<string> Summary { get; } = new List<string>(); // List: a resizable collection for the end-of-game messages.

        // COMPOSITION: the engine is built from helper objects that each handle part of the game.
        public DisasterManager Map { get; } = new DisasterManager();
        public EncounterDatabase Database { get; } = new EncounterDatabase();
        public Player Player { get; private set; }
        public List<FamilyMember> Family { get; } = new List<FamilyMember>(); // A list lets us loop through each family member.
        public FamilyMember Target { get; private set; }
        public Scenario Scenario { get; private set; }
        public string ResultText { get; private set; } = "";

        public bool ExitOpen
        {
            get
            {
                for (int i = 0; i < Family.Count; i++)
                {
                    if (!Family[i].Rescued)
                        return false;
                }
                return true;
            }
        }

        public int RescuedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Family.Count; i++)
                {
                    if (Family[i].Rescued)
                        count++;
                }
                return count;
            }
        }

        public event Action Changed;            // Event: a notification with no extra data when the game changes.
        public event Action EncounterStarted;
        public event Action Ended;

        public void StartLevel(Difficulty d)
        {
            Disaster = d;
            Map.Load(d);

            lives = MaxLives;
            score = 0;
            combo = 0;
            Ticks = 0;
            Hits = 0;

            Database.MissedLessons.Clear();
            Player = new Player(Map.Start.X, Map.Start.Y);

            Family.Clear();
            foreach (KeyValuePair<FamilyType, Point> spot in Map.FamilySpots) // foreach visits each family-type/map-position pair.
            {
                Family.Add(new FamilyMember(spot.Key, spot.Value.X, spot.Value.Y));
            }

            State = GameState.Walking;
            Changed?.Invoke(); // ?. calls the event only if a listener has subscribed.
        }

        public void MovePlayer(int dx, int dy)
        {
            if (State != GameState.Walking)
                return;

            int nx = Player.X + dx;
            int ny = Player.Y + dy;
            if (!Map.IsWalkable(nx, ny))
                return;

            prevX = Player.X;
            prevY = Player.Y;
            Player.MoveTo(nx, ny);

            CheckHazard();
            if (State != GameState.Walking)
                return;

            // Tingnan kung may kapamilya sa nilipatang tile
            FamilyMember foundMember = null;
            for (int i = 0; i < Family.Count; i++)
            {
                if (!Family[i].Rescued && Family[i].IsAt(nx, ny))
                {
                    foundMember = Family[i];
                    break;
                }
            }

            if (foundMember != null)
            {
                StartEncounter(foundMember);
            }
            else if (ExitOpen && Map.Exit.X == nx && Map.Exit.Y == ny)
            {
                EndLevel(true);
            }
        }

        // tinatawag ng MainForm kada 100 ms
        public void Tick()
        {
            if (State != GameState.Walking)
                return;

            Ticks++;
            if (Player.Invincible > 0)
            {
                Player.Invincible--;
            }

            Map.Tick(Player.X, Player.Y);
            CheckHazard();
        }

        private void CheckHazard()
        {
            if (Player.Invincible > 0 || !Map.IsDanger(Player.X, Player.Y))
                return;

            Hits++;
            Player.Invincible = 15;   // 1.5 s na kumikislap
            LoseLife();
        }

        private void LoseLife()
        {
            lives--;
            combo = 0;
            Changed?.Invoke();

            if (lives <= 0 && State == GameState.Walking)
            {
                EndLevel(false);
            }
        }

        private void StartEncounter(FamilyMember f)
        {
            Target = f;
            Scenario = Database.Draw(Disaster, f.Type);
            State = GameState.Encounter;
            EncounterStarted?.Invoke(); // Notify the UI so it can show the rescue cards.
        }

        // tinatawag ng UI: index 0-2 = card na pinili, -1 = naubos ang oras
        public CardResult ChooseCard(int index, int secondsLeft)
        {
            CardResult result = Database.CheckAnswer(Scenario, index);

            if (result != CardResult.Handa)
            {
                combo = 0;
                Database.RecordMiss(Scenario.BestCard.Lesson);
            }

            switch (result) // Choose the matching game rule for the answer result.
            {
                case CardResult.Handa:
                    ApplyHandaResult(index, secondsLeft);
                    break;
                case CardResult.PwedeNa:
                    ApplyPwedeNaResult();
                    break;
                case CardResult.Sablay:
                default:
                    ApplySablayResult();
                    break;
            }

            Changed?.Invoke();
            return result;
        }

        private void ApplyHandaResult(int cardIndex, int secondsLeft)
        {
            combo++;
            int points = Scenario.Cards[cardIndex].GetPoints(Multiplier, secondsLeft);
            score += points;
            ResultText = "+" + points + (combo > 1 ? "   COMBO x" + Multiplier : "");

            if (combo % 3 == 0 && lives < MaxLives)   // Bayanihan Bonus
            {
                lives++;
                ResultText += "   BAYANIHAN BONUS +1 PUSO";
            }

            Target.Rescued = true;
        }

        private void ApplyPwedeNaResult()
        {
            score += 40;
            ResultText = "+40   (combo reset)";
            Target.Rescued = true;
        }

        private void ApplySablayResult()
        {
            lives--;
            ResultText = "-1 PUSO   (subukan ulit)";
            Player.MoveTo(prevX, prevY);   // balik sa dating tile
        }

        // tinatawag pagkatapos pindutin ang TULOY
        public void Resume()
        {
            Target = null;
            if (lives <= 0)
            {
                EndLevel(false);
                return;
            }

            State = GameState.Walking;
            Player.Invincible = 10;
        }

        private void EndLevel(bool cleared)
        {
            State = cleared ? GameState.Clear : GameState.GameOver;
            Summary.Clear();
            Stars = 0;

            if (cleared)
            {
                int timeBonus = Math.Max(0, 90 - Seconds) * 5;
                score += 200 + timeBonus;
                Summary.Add("Nakarating sa EVAC: +200");
                Summary.Add("Bilis (" + Seconds + "s): +" + timeBonus);

                if (Hits == 0)
                {
                    score += 300;
                    Summary.Add("Walang galos: +300");
                }

                bool allHanda = Database.MissedLessons.Count == 0;
                bool noHits = Hits == 0;
                bool fast = Seconds <= 90;

                Stars = (allHanda ? 1 : 0) + (noHits ? 1 : 0) + (fast ? 1 : 0);
                Summary.Add((allHanda ? "★" : "☆") + " Puro HANDA ang pinili");
                Summary.Add((noHits ? "★" : "☆") + " Walang tama ng sakuna");
                Summary.Add((fast ? "★" : "☆") + " Natapos sa loob ng 90 segundo");
            }
            else
            {
                // DITO MO ILAGAY ANG MENSAHE KAPAG NATALO KA:
                Summary.Add("NABIGO KA! Hindi ka naging handa sa sakuna.");
                Summary.Add("Kulang ang iyong kaalaman sa kaligtasan.");
            }

            Ended?.Invoke();
        }
    }
}