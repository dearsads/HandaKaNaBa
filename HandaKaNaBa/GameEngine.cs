// GameEngine.cs - Member 2 (Grid Mechanics)
// Ang "utak" ng laro: player, puso, score, combo, collision at resulta ng cards.
// Hindi ito nagdo-drawing. Sinasabihan lang nito ang UI gamit ang EVENTS.
namespace HandaKaNaBa
{
    public enum GameState { Menu, Walking, Encounter, Clear, GameOver }

    // ABSTRACT parent ng lahat ng nasa grid
    public abstract class GridEntity
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
        protected GridEntity(int x, int y) { X = x; Y = y; }
        public abstract string Sprite { get; }
        public bool IsAt(int x, int y) => X == x && Y == y;
    }

    public class Player : GridEntity
    {
        public int Invincible { get; set; }   // ticks na hindi tatamaan pagkatapos masaktan
        public Player(int x, int y) : base(x, y) { }
        public override string Sprite => "player";
        public void MoveTo(int x, int y) { X = x; Y = y; }
    }

    public class FamilyMember : GridEntity
    {
        public FamilyType Type { get; }
        public bool Rescued { get; set; }
        public FamilyMember(FamilyType type, int x, int y) : base(x, y) { Type = type; }
        public override string Sprite => Type.ToString().ToLower();   // lola, bunso, bantay
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

        public GameState State { get; private set; } = GameState.Menu;
        public Difficulty Disaster { get; private set; }
        public int Lives => lives;
        public int Score => score;
        public int Multiplier => Math.Max(1, Math.Min(combo, 3));
        public int Ticks { get; private set; }
        public int Seconds => Ticks / 10;
        public int Hits { get; private set; }
        public int Stars { get; private set; }
        public List<string> Summary { get; } = new List<string>();

        // COMPOSITION: ang engine ang may-ari ng mapa, database, player at pamilya
        public DisasterManager Map { get; } = new DisasterManager();
        public EncounterDatabase Database { get; } = new EncounterDatabase();
        public Player Player { get; private set; }
        public List<FamilyMember> Family { get; } = new List<FamilyMember>();
        public FamilyMember Target { get; private set; }
        public Scenario Scenario { get; private set; }
        public string ResultText { get; private set; } = "";

        public bool ExitOpen => Family.All(f => f.Rescued);
        public int RescuedCount => Family.Count(f => f.Rescued);

        public event Action Changed;            // nagbago ang puso o score
        public event Action EncounterStarted;
        public event Action Ended;

        public void StartLevel(Difficulty d)
        {
            Disaster = d;
            Map.Load(d);
            lives = MaxLives; score = 0; combo = 0; Ticks = 0; Hits = 0;
            Database.MissedLessons.Clear();
            Player = new Player(Map.Start.X, Map.Start.Y);
            Family.Clear();
            foreach (var spot in Map.FamilySpots)
                Family.Add(new FamilyMember(spot.Key, spot.Value.X, spot.Value.Y));
            State = GameState.Walking;
            Changed?.Invoke();
        }

        public void MovePlayer(int dx, int dy)
        {
            if (State != GameState.Walking) return;
            int nx = Player.X + dx, ny = Player.Y + dy;
            if (!Map.IsWalkable(nx, ny)) return;

            prevX = Player.X; prevY = Player.Y;
            Player.MoveTo(nx, ny);
            CheckHazard();
            if (State != GameState.Walking) return;

            FamilyMember f = Family.FirstOrDefault(m => !m.Rescued && m.IsAt(nx, ny));
            if (f != null) StartEncounter(f);
            else if (ExitOpen && Map.Exit == new Point(nx, ny)) EndLevel(true);
        }

        // tinatawag ng MainForm kada 100 ms
        public void Tick()
        {
            if (State != GameState.Walking) return;
            Ticks++;
            if (Player.Invincible > 0) Player.Invincible--;
            Map.Tick(Player.X, Player.Y);
            CheckHazard();
        }

        private void CheckHazard()
        {
            if (Player.Invincible > 0 || !Map.IsDanger(Player.X, Player.Y)) return;
            Hits++;
            Player.Invincible = 15;   // 1.5 s na kumikislap
            LoseLife();
        }

        private void LoseLife()
        {
            lives--; combo = 0;
            Changed?.Invoke();
            if (lives <= 0 && State == GameState.Walking) EndLevel(false);
        }

        private void StartEncounter(FamilyMember f)
        {
            Target = f;
            Scenario = Database.Draw(Disaster, f.Type);
            State = GameState.Encounter;
            EncounterStarted?.Invoke();
        }

        // tinatawag ng UI: index 0-2 = card na pinili, -1 = naubos ang oras
        public CardResult ChooseCard(int index, int secondsLeft)
        {
            CardResult result = Database.CheckAnswer(Scenario, index);

            if (result == CardResult.Handa)
            {
                combo++;
                int points = Scenario.Cards[index].GetPoints(Multiplier, secondsLeft);
                score += points;
                ResultText = "+" + points + (combo > 1 ? "   COMBO x" + Multiplier : "");
                if (combo % 3 == 0 && lives < MaxLives)   // Bayanihan Bonus
                {
                    lives++;
                    ResultText += "   BAYANIHAN BONUS +1 PUSO";
                }
                Target.Rescued = true;
            }
            else   // PWEDE NA o SABLAY: reset ang combo at itatala ang aral
            {
                combo = 0;
                Database.RecordMiss(Scenario.BestCard.Lesson);
                if (result == CardResult.PwedeNa) { score += 40; ResultText = "+40   (combo reset)"; Target.Rescued = true; }
                else { lives--; ResultText = "-1 PUSO   (subukan ulit)"; Player.MoveTo(prevX, prevY); }   // balik sa dating tile
            }
            Changed?.Invoke();
            return result;
        }

        // tinatawag pagkatapos pindutin ang TULOY
        public void Resume()
        {
            Target = null;
            if (lives <= 0) { EndLevel(false); return; }
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
                if (Hits == 0) { score += 300; Summary.Add("Walang galos: +300"); }

                bool allHanda = Database.MissedLessons.Count == 0, noHits = Hits == 0, fast = Seconds <= 90;
                Stars = (allHanda ? 1 : 0) + (noHits ? 1 : 0) + (fast ? 1 : 0);
                Summary.Add((allHanda ? "★" : "☆") + " Puro HANDA ang pinili");
                Summary.Add((noHits ? "★" : "☆") + " Walang tama ng sakuna");
                Summary.Add((fast ? "★" : "☆") + " Natapos sa loob ng 90 segundo");
            }
            Ended?.Invoke();
        }
    }
}
