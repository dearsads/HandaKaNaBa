// DisasterManager.cs - Member 2 (Grid Mechanics)
// Ang 12x12 na mapa at ang 3 sakuna. 1 tick = 100 ms, kaya 10 ticks = 1 segundo.
namespace HandaKaNaBa
{
    public enum Tile { Floor, Wall, High, Evac } // Enum: each map cell has one named kind of terrain.

    // ABSTRACT parent class: bawat sakuna may sariling Update, IsWarning at IsDanger
    public abstract class Hazard // Abstraction: all disasters answer the same warning/danger questions.
    {
        protected DisasterManager map;
        protected Random rng = new Random(); // protected means each disaster child can use this random-number generator.
        protected int age;   // ilang ticks na ang lumipas

        protected Hazard(DisasterManager map)
        {
            this.map = map;
        }

        public abstract string Name { get; }
        public abstract string DangerSprite { get; }
        public abstract void Update(int playerX, int playerY); // Each disaster must define how it changes over time.
        public abstract bool IsWarning(int x, int y);   // Abstract method: each disaster decides which tiles warn the player.
        public abstract bool IsDanger(int x, int y);    // Abstract method: each disaster decides which tiles cause damage.
    }

    // TSUNAMI: alon na 3 column ang kapal, galing sa kaliwa. Ligtas sa ^ (burol).
    public class Tsunami : Hazard // Inheritance: tsunami follows the shared Hazard interface and rules.
    {
        private int front = -20;   // column ng alon; negative = pahinga pa
        private int waves = 0;

        public Tsunami(DisasterManager map) : base(map) { }

        public override string Name => "TSUNAMI";
        public override string DangerSprite => "water";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int speed = waves >= 3 ? 2 : 3;   // bumibilis pagkatapos ng 3 alon
            if (age % speed == 0)
            {
                front++;
            }

            if (front > 14)   // lumabas na ang alon, pahinga muna
            {
                waves++;
                front = -Math.Max(10, 20 - waves * 3);
            }
        }

        // kumikislap ang 2 column sa dagat bago dumating, tapos ang column na susunod na tatamaan
        public override bool IsWarning(int x, int y)
        {
            if (map.GetTile(x, y) == Tile.High)
                return false;

            if (front < 0 && front >= -6)
                return x < 2;

            return x == front + 1;
        }

        public override bool IsDanger(int x, int y)
        {
            if (map.GetTile(x, y) == Tile.High)
                return false;

            return front >= 0 && x <= front && x > front - 3;
        }
    }

    // LINDOL: bitak sa row o column. Habang tumatagal, mas madalas at mas mahaba.
    public class Lindol : Hazard // Each child overrides the hazard behavior in its own way.
    {
        private class Crack
        {
            public List<Point> Cells = new List<Point>();
            public int Born;

            public bool HasCell(int x, int y)
            {
                for (int i = 0; i < Cells.Count; i++)
                {
                    if (Cells[i].X == x && Cells[i].Y == y)
                        return true;
                }
                return false;
            }
        }

        private readonly List<Crack> cracks = new List<Crack>();

        public Lindol(DisasterManager map) : base(map) { }

        public override string Name => "LINDOL";
        public override string DangerSprite => "crack";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(10, 30 - age / 60);   // 3 s sa simula, 1 s sa huli
            if (age % every == 0)
            {
                AddCrack();
            }

            // Tanggalin ang mga bitak na lampas 2.7 seconds (27 ticks) na
            for (int i = cracks.Count - 1; i >= 0; i--)
            {
                if (age - cracks[i].Born > 27)
                    cracks.RemoveAt(i);
            }
        }

        private void AddCrack()
        {
            int length = Math.Min(7, 3 + age / 150);   // +1 tile kada 15 s
            bool horizontal = rng.Next(2) == 0;
            int line = rng.Next(12);
            int start = rng.Next(12 - length + 1);

            Crack crack = new Crack { Born = age };
            for (int i = 0; i < length; i++)
            {
                Point cell = horizontal ? new Point(start + i, line) : new Point(line, start + i);
                crack.Cells.Add(cell);
            }
            cracks.Add(crack);
        }

        public override bool IsWarning(int x, int y)
        {
            for (int i = 0; i < cracks.Count; i++)
            {
                if (age - cracks[i].Born < 12 && cracks[i].HasCell(x, y))
                    return true;
            }
            return false;
        }

        public override bool IsDanger(int x, int y)
        {
            for (int i = 0; i < cracks.Count; i++)
            {
                if (age - cracks[i].Born >= 12 && cracks[i].HasCell(x, y))
                    return true;
            }
            return false;
        }
    }

    // EL NIÑO: 3x3 na lupa na natutuyo. Berde -> kayumanggi -> tuyong lupa (delikado).
    public class ElNino : Hazard
    {
        private class Patch
        {
            public int X, Y, Born;
        }

        private readonly List<Patch> patches = new List<Patch>();

        public ElNino(DisasterManager map) : base(map) { }

        public override string Name => "EL NIÑO";
        public override string DangerSprite => "dry";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(12, 28 - age / 75);
            int count = Math.Min(3, 1 + age / 300);   // dumadami kada 30 s

            if (age % every == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bool nearPlayer = rng.Next(100) < 60;   // 60% malapit sa player
                    int x = nearPlayer ? playerX + rng.Next(-3, 4) : rng.Next(12);
                    int y = nearPlayer ? playerY + rng.Next(-3, 4) : rng.Next(12);

                    patches.Add(new Patch
                    {
                        X = Math.Clamp(x, 0, 11),
                        Y = Math.Clamp(y, 0, 11),
                        Born = age
                    });
                }
            }

            // Tanggalin ang mga tuyong lupa na lampas 3.2 seconds (32 ticks) na
            for (int i = patches.Count - 1; i >= 0; i--)
            {
                if (age - patches[i].Born > 32)
                    patches.RemoveAt(i);
            }
        }

        private bool InPatch(Patch p, int x, int y)
        {
            return Math.Abs(p.X - x) <= 1 && Math.Abs(p.Y - y) <= 1;
        }

        public override bool IsWarning(int x, int y)
        {
            for (int i = 0; i < patches.Count; i++)
            {
                if (age - patches[i].Born < 18 && InPatch(patches[i], x, y))
                    return true;
            }
            return false;
        }

        public override bool IsDanger(int x, int y)
        {
            for (int i = 0; i < patches.Count; i++)
            {
                if (age - patches[i].Born >= 18 && InPatch(patches[i], x, y))
                    return true;
            }
            return false;
        }
    }

    public class DisasterManager
    {
        private const int GridSize = 12;
        private readonly Tile[,] tiles = new Tile[GridSize, GridSize];   // 2D array: one tile value for each x/y map coordinate.

        public Hazard Current { get; private set; } // Polymorphism: this one variable can refer to any selected Hazard child.
        public Point Start { get; private set; }
        public Point Exit { get; private set; }
        public Dictionary<FamilyType, Point> FamilySpots { get; } = new Dictionary<FamilyType, Point>(); // Dictionary pairs a family-type key with its map-position value.

        public void Load(Difficulty d)
        {
            string[] rows = EncounterDatabase.GetMap(d); // Array: each string is one row of the map description.
            FamilySpots.Clear();

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    char c = rows[y][x];
                    Tile t = Tile.Floor;

                    switch (c)
                    {
                        case '#':
                            t = Tile.Wall;
                            break;
                        case '^':
                            t = Tile.High;
                            break;
                        case 'E':
                            t = Tile.Evac;
                            Exit = new Point(x, y);
                            break;
                        case 'P':
                            t = Tile.High;
                            Start = new Point(x, y);
                            break;
                        case 'L':
                            t = Tile.High;
                            FamilySpots[FamilyType.Lola] = new Point(x, y);
                            break;
                        case 'B':
                            t = Tile.High;
                            FamilySpots[FamilyType.Bunso] = new Point(x, y);
                            break;
                        case 'D':
                            t = Tile.High;
                            FamilySpots[FamilyType.Bantay] = new Point(x, y);
                            break;
                    }

                    tiles[x, y] = t;
                }
            }

            if (d == Difficulty.Tsunami) // Create the matching Hazard child; later calls work through Current.
                Current = new Tsunami(this);
            else if (d == Difficulty.Lindol)
                Current = new Lindol(this);
            else
                Current = new ElNino(this);
        }

        // labas ng mapa = pader
        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize)
                return Tile.Wall;

            return tiles[x, y];
        }

        public bool IsWalkable(int x, int y) => GetTile(x, y) != Tile.Wall;
        public void Tick(int px, int py) => Current.Update(px, py); // Expression-bodied method: short syntax for one statement.

        // laging ligtas ang evacuation center
        public bool IsDanger(int x, int y) => GetTile(x, y) != Tile.Evac && Current.IsDanger(x, y);
        public bool IsWarning(int x, int y) => GetTile(x, y) != Tile.Evac && Current.IsWarning(x, y);
    }
}