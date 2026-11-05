// DisasterManager.cs - Member 2 (Grid Mechanics)
// Ang 12x12 na mapa at ang 3 sakuna. 1 tick = 100 ms, kaya 10 ticks = 1 segundo.
namespace HandaKaNaBa
{
    public enum Tile { Floor, Wall, High, Evac }

    // ABSTRACT parent class: bawat sakuna may sariling Update, IsWarning at IsDanger
    public abstract class Hazard
    {
        protected DisasterManager map;
        protected Random rng = new Random();
        protected int age;   // ilang ticks na ang lumipas

        protected Hazard(DisasterManager map) { this.map = map; }

        public abstract string Name { get; }
        public abstract string DangerSprite { get; }
        public abstract void Update(int playerX, int playerY);
        public abstract bool IsWarning(int x, int y);   // kumikislap: umalis ka na!
        public abstract bool IsDanger(int x, int y);    // -1 puso
    }

    // TSUNAMI: alon na 3 column ang kapal, galing sa kaliwa. Ligtas sa ^ (burol).
    public class Tsunami : Hazard
    {
        private int front = -20;   // column ng alon; negative = pahinga pa
        private int waves = 0;

        public Tsunami(DisasterManager map) : base(map) { }
        public override string Name => "TSUNAMI";
        public override string DangerSprite => "water";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int speed = waves >= 3 ? 2 : 3;      // bumibilis pagkatapos ng 3 alon
            if (age % speed == 0) front++;
            if (front > 14)                      // lumabas na ang alon, pahinga muna
            {
                waves++;
                front = -Math.Max(10, 20 - waves * 3);
            }
        }

        // kumikislap ang 2 column sa dagat bago dumating, tapos ang column na susunod na tatamaan
        public override bool IsWarning(int x, int y) =>
            map.GetTile(x, y) != Tile.High && (front < 0 && front >= -6 ? x < 2 : x == front + 1);

        public override bool IsDanger(int x, int y) =>
            front >= 0 && x <= front && x > front - 3 && map.GetTile(x, y) != Tile.High;
    }

    // LINDOL: bitak sa row o column. Habang tumatagal, mas madalas at mas mahaba.
    public class Lindol : Hazard
    {
        private class Crack { public List<Point> Cells = new List<Point>(); public int Born; }
        private List<Crack> cracks = new List<Crack>();

        public Lindol(DisasterManager map) : base(map) { }
        public override string Name => "LINDOL";
        public override string DangerSprite => "crack";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(10, 30 - age / 60);   // 3 s sa simula, 1 s sa huli
            if (age % every == 0) AddCrack();
            cracks.RemoveAll(c => age - c.Born > 27);
        }

        private void AddCrack()
        {
            int length = Math.Min(7, 3 + age / 150);   // +1 tile kada 15 s
            bool horizontal = rng.Next(2) == 0;
            int line = rng.Next(12);
            int start = rng.Next(12 - length + 1);
            var crack = new Crack { Born = age };
            for (int i = 0; i < length; i++)
                crack.Cells.Add(horizontal ? new Point(start + i, line) : new Point(line, start + i));
            cracks.Add(crack);
        }

        public override bool IsWarning(int x, int y) => cracks.Any(c => age - c.Born < 12 && c.Cells.Contains(new Point(x, y)));
        public override bool IsDanger(int x, int y) => cracks.Any(c => age - c.Born >= 12 && c.Cells.Contains(new Point(x, y)));
    }

    // EL NIÑO: 3x3 na lupa na natutuyo. Berde -> kayumanggi -> tuyong lupa (delikado).
    public class ElNino : Hazard
    {
        private class Patch { public int X, Y, Born; }
        private List<Patch> patches = new List<Patch>();

        public ElNino(DisasterManager map) : base(map) { }
        public override string Name => "EL NIÑO";
        public override string DangerSprite => "dry";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(12, 28 - age / 75);
            int count = Math.Min(3, 1 + age / 300);    // dumadami kada 30 s
            for (int i = 0; age % every == 0 && i < count; i++)
            {
                bool nearPlayer = rng.Next(100) < 60;   // 60% malapit sa player
                int x = nearPlayer ? playerX + rng.Next(-3, 4) : rng.Next(12);
                int y = nearPlayer ? playerY + rng.Next(-3, 4) : rng.Next(12);
                patches.Add(new Patch { X = Math.Clamp(x, 0, 11), Y = Math.Clamp(y, 0, 11), Born = age });
            }
            patches.RemoveAll(p => age - p.Born > 32);
        }

        private bool InPatch(Patch p, int x, int y) => Math.Abs(p.X - x) <= 1 && Math.Abs(p.Y - y) <= 1;

        public override bool IsWarning(int x, int y) => patches.Any(p => age - p.Born < 18 && InPatch(p, x, y));
        public override bool IsDanger(int x, int y) => patches.Any(p => age - p.Born >= 18 && InPatch(p, x, y));
    }

    public class DisasterManager
    {
        private Tile[,] tiles = new Tile[12, 12];   // tiles[x, y]
        public Hazard Current { get; private set; }
        public Point Start { get; private set; }
        public Point Exit { get; private set; }
        public Dictionary<FamilyType, Point> FamilySpots { get; } = new Dictionary<FamilyType, Point>();

        public void Load(Difficulty d)
        {
            string[] rows = EncounterDatabase.GetMap(d);
            FamilySpots.Clear();
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    char c = rows[y][x];
                    Tile t = Tile.Floor;
                    if (c == '#') t = Tile.Wall;
                    else if (c == '^') t = Tile.High;
                    else if (c == 'E') { t = Tile.Evac; Exit = new Point(x, y); }
                    else if (c == 'P') { t = Tile.High; Start = new Point(x, y); }
                    else if (c == 'L') { t = Tile.High; FamilySpots[FamilyType.Lola] = new Point(x, y); }
                    else if (c == 'B') { t = Tile.High; FamilySpots[FamilyType.Bunso] = new Point(x, y); }
                    else if (c == 'D') { t = Tile.High; FamilySpots[FamilyType.Bantay] = new Point(x, y); }
                    tiles[x, y] = t;
                }
            }

            if (d == Difficulty.Tsunami) Current = new Tsunami(this);
            else if (d == Difficulty.Lindol) Current = new Lindol(this);
            else Current = new ElNino(this);
        }

        // labas ng mapa = pader
        public Tile GetTile(int x, int y) => (x < 0 || x >= 12 || y < 0 || y >= 12) ? Tile.Wall : tiles[x, y];

        public bool IsWalkable(int x, int y) => GetTile(x, y) != Tile.Wall;
        public void Tick(int px, int py) => Current.Update(px, py);

        // laging ligtas ang evacuation center
        public bool IsDanger(int x, int y) => GetTile(x, y) != Tile.Evac && Current.IsDanger(x, y);
        public bool IsWarning(int x, int y) => GetTile(x, y) != Tile.Evac && Current.IsWarning(x, y);
    }
}
