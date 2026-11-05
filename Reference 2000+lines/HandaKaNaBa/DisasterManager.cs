// =====================================================================
//  DisasterManager.cs
//  OWNER: Member 2 - Back-End (Grid Mechanics)
//
//  The 12x12 map (terrain only) and the 3 disasters.
//  1 tick = 100 ms (see MainForm's game loop).
//
//  OOP: Hazard is ABSTRACT. TsunamiHazard, EarthquakeHazard and
//  VolcanoHazard INHERIT from it and OVERRIDE Update / IsWarning /
//  IsDanger (POLYMORPHISM). GameEngine never needs an
//  "if tsunami ... else if earthquake ..." chain.
// =====================================================================
namespace HandaKaNaBa
{
    public enum Tile { Floor, Wall, High, Evac, Fissure }

    // ------------------------------------------------------------------
    //  BASE HAZARD
    // ------------------------------------------------------------------
    public abstract class Hazard
    {
        protected readonly DisasterManager Map;
        protected readonly Random Rng = new Random();
        protected int Age;                                   // ticks since the level started

        protected Hazard(DisasterManager map)
        {
            Map = map;
        }

        public abstract string Name { get; }
        public abstract string DangerSprite { get; }         // sprite drawn on dangerous tiles
        public virtual string Banner => null;                // big warning text, null = none

        /// <summary>Danger level shown in the HUD. Goes up every 30 seconds.</summary>
        public int Intensity => 1 + Age / 300;

        public abstract void Update(int playerX, int playerY);
        public abstract bool IsWarning(int x, int y);        // flashing: get away!
        public abstract bool IsDanger(int x, int y);         // costs a heart
    }

    // ------------------------------------------------------------------
    //  TSUNAMI (Easy): a 3-column wave sweeps left to right.
    //  High ground (^) is safe. Waves come faster over time.
    // ------------------------------------------------------------------
    public class TsunamiHazard : Hazard
    {
        private enum Phase { Calm, Warning, Wave }

        private const int Thickness = 3;
        private const int WarningTicks = 20;

        private Phase phase = Phase.Calm;
        private int phaseTicks;
        private int front = -1;      // right-most column of the wave
        private int waves;

        public TsunamiHazard(DisasterManager map) : base(map) { }

        public override string Name => "TSUNAMI";
        public override string DangerSprite => "water";
        public override string Banner => phase == Phase.Warning ? "UMATRAS ANG DAGAT! UMAKYAT SA BUROL!" : null;

        private int CalmTicks => Math.Max(30, 70 - waves * 8);   // shorter rest after every wave
        private int StepTicks => waves >= 3 ? 2 : 3;               // faster wave after 3 waves

        public override void Update(int playerX, int playerY)
        {
            Age++;
            phaseTicks++;
            switch (phase)
            {
                case Phase.Calm:
                    if (phaseTicks >= CalmTicks) { phase = Phase.Warning; phaseTicks = 0; }
                    break;
                case Phase.Warning:
                    if (phaseTicks >= WarningTicks) { phase = Phase.Wave; phaseTicks = 0; front = -1; }
                    break;
                case Phase.Wave:
                    if (phaseTicks % StepTicks == 0) front++;
                    if (front - Thickness >= DisasterManager.Size) { phase = Phase.Calm; phaseTicks = 0; waves++; }
                    break;
            }
        }

        private bool InWave(int x) => x <= front && x > front - Thickness;

        public override bool IsWarning(int x, int y)
        {
            if (Map.GetTile(x, y) == Tile.High) return false;
            if (phase == Phase.Warning) return x < 2;              // the sea side flashes
            return phase == Phase.Wave && x == front + 1;          // the column the wave hits next
        }

        public override bool IsDanger(int x, int y)
        {
            return phase == Phase.Wave && InWave(x) && Map.GetTile(x, y) != Tile.High;
        }
    }

    // ------------------------------------------------------------------
    //  EARTHQUAKE (Medium): cracks in rows/columns. DYNAMIC:
    //   - cracks come faster and get longer the longer you take
    //   - some cracks stay forever as fissures (walls), but never if
    //     that would trap the player or cut off a family member
    //   - an aftershock every 25 s spawns 3 cracks at once
    // ------------------------------------------------------------------
    public class EarthquakeHazard : Hazard
    {
        private class Crack
        {
            public readonly List<Point> Cells = new List<Point>();
            public int Age;
        }

        private const int WarnTicks = 12;
        private const int ActiveTicks = 15;

        private readonly List<Crack> cracks = new List<Crack>();
        private int nextSpawn = 20;
        private string banner;
        private int bannerTicks;

        public EarthquakeHazard(DisasterManager map) : base(map) { }

        public override string Name => "LINDOL";
        public override string DangerSprite => "crack";
        public override string Banner => bannerTicks > 0 ? banner : null;

        private int SpawnEvery => Math.Max(10, 30 - Age / 60);       // 3 s at the start, 1 s at the end
        private int CrackLength => Math.Min(7, 3 + Age / 150);         // +1 tile every 15 s
        private int PermanentChance => Math.Min(45, 15 + Age / 60);    // % chance a crack stays

        public override void Update(int playerX, int playerY)
        {
            Age++;
            if (bannerTicks > 0) bannerTicks--;

            if (--nextSpawn <= 0)
            {
                SpawnCrack();
                nextSpawn = SpawnEvery;
            }

            if (Age % 250 == 0)
            {
                for (int i = 0; i < 3; i++) SpawnCrack();
                banner = "AFTERSHOCK!";
                bannerTicks = 15;
            }

            foreach (Crack c in cracks) c.Age++;

            foreach (Crack done in cracks.Where(c => c.Age >= WarnTicks + ActiveTicks).ToList())
            {
                if (Rng.Next(100) < PermanentChance && Map.TryMakeFissure(done.Cells))
                {
                    banner = "LUMALAKI ANG BITAK!";
                    bannerTicks = 10;
                }
                cracks.Remove(done);
            }
        }

        private void SpawnCrack()
        {
            bool horizontal = Rng.Next(2) == 0;
            int length = CrackLength;
            int line = Rng.Next(DisasterManager.Size);
            int start = Rng.Next(DisasterManager.Size - length + 1);

            var crack = new Crack();
            for (int i = 0; i < length; i++)
                crack.Cells.Add(horizontal ? new Point(start + i, line) : new Point(line, start + i));
            cracks.Add(crack);
        }

        public override bool IsWarning(int x, int y) =>
            cracks.Any(c => c.Age < WarnTicks && c.Cells.Contains(new Point(x, y)));

        public override bool IsDanger(int x, int y) =>
            cracks.Any(c => c.Age >= WarnTicks && c.Cells.Contains(new Point(x, y)));
    }

    // ------------------------------------------------------------------
    //  VOLCANO (Hard): 3x3 blasts, most of them near the player.
    //  More blasts at once every 30 s.
    // ------------------------------------------------------------------
    public class VolcanoHazard : Hazard
    {
        private class Blast
        {
            public int X, Y, Age;
        }

        private const int WarnTicks = 18;
        private const int ActiveTicks = 14;

        private readonly List<Blast> blasts = new List<Blast>();
        private int nextSpawn = 25;

        public VolcanoHazard(DisasterManager map) : base(map) { }

        public override string Name => "BULKAN";
        public override string DangerSprite => "lava";
        public override string Banner => Age % 300 >= 280 ? "LUMALAKAS ANG PAGSABOG!" : null;

        private int SpawnEvery => Math.Max(12, 28 - Age / 75);
        private int BlastsPerWave => Math.Min(3, 1 + Age / 300);

        public override void Update(int playerX, int playerY)
        {
            Age++;
            if (--nextSpawn <= 0)
            {
                for (int i = 0; i < BlastsPerWave; i++)
                {
                    bool nearPlayer = Rng.Next(100) < 60;
                    int x = nearPlayer ? playerX + Rng.Next(-3, 4) : Rng.Next(DisasterManager.Size);
                    int y = nearPlayer ? playerY + Rng.Next(-3, 4) : Rng.Next(DisasterManager.Size);
                    blasts.Add(new Blast
                    {
                        X = Math.Clamp(x, 0, DisasterManager.Size - 1),
                        Y = Math.Clamp(y, 0, DisasterManager.Size - 1)
                    });
                }
                nextSpawn = SpawnEvery;
            }

            foreach (Blast b in blasts) b.Age++;
            blasts.RemoveAll(b => b.Age >= WarnTicks + ActiveTicks);
        }

        private static bool InBlast(Blast b, int x, int y) => Math.Abs(b.X - x) <= 1 && Math.Abs(b.Y - y) <= 1;

        public override bool IsWarning(int x, int y) => blasts.Any(b => b.Age < WarnTicks && InBlast(b, x, y));
        public override bool IsDanger(int x, int y) => blasts.Any(b => b.Age >= WarnTicks && InBlast(b, x, y));
    }

    // ------------------------------------------------------------------
    //  THE MAP
    // ------------------------------------------------------------------
    public class DisasterManager
    {
        public const int Size = 12;

        /// <summary>Most permanent fissure tiles allowed, so the map never fills up.</summary>
        public const int MaxFissures = 30;

        private readonly Tile[,] map = new Tile[Size, Size];     // map[x, y]: x = column, y = row

        public Hazard Current { get; private set; }
        public Point Start { get; private set; }
        public Point Exit { get; private set; }
        public Point GoBag { get; private set; } = new Point(-1, -1);
        public Dictionary<FamilyType, Point> FamilyStarts { get; } = new Dictionary<FamilyType, Point>();
        public int FissureCount { get; private set; }

        /// <summary>Set by GameEngine: tiles that must always stay reachable (player, exit, family...).</summary>
        public Func<IEnumerable<Point>> ImportantPoints { get; set; }

        public void Load(LevelData level)
        {
            FamilyStarts.Clear();
            GoBag = new Point(-1, -1);
            FissureCount = 0;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    char c = level.Rows[y][x];
                    Tile tile = Tile.Floor;
                    switch (c)
                    {
                        case '#': tile = Tile.Wall; break;
                        case '^': tile = Tile.High; break;
                        case 'E': tile = Tile.Evac; Exit = new Point(x, y); break;
                        case 'P': tile = Tile.High; Start = new Point(x, y); break;
                        case 'L': tile = Tile.High; FamilyStarts[FamilyType.Lola] = new Point(x, y); break;
                        case 'B': tile = Tile.High; FamilyStarts[FamilyType.Bunso] = new Point(x, y); break;
                        case 'D': tile = Tile.High; FamilyStarts[FamilyType.Bantay] = new Point(x, y); break;
                        case 'G': GoBag = new Point(x, y); break;
                    }
                    map[x, y] = tile;
                }
            }

            Current = CreateHazard(level.Disaster);
        }

        // Factory: the only place that knows which class goes with which disaster.
        private Hazard CreateHazard(Difficulty disaster)
        {
            switch (disaster)
            {
                case Difficulty.Tsunami: return new TsunamiHazard(this);
                case Difficulty.Earthquake: return new EarthquakeHazard(this);
                default: return new VolcanoHazard(this);
            }
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

        public Tile GetTile(int x, int y) => InBounds(x, y) ? map[x, y] : Tile.Wall;

        public bool IsWalkable(int x, int y)
        {
            Tile t = GetTile(x, y);
            return InBounds(x, y) && t != Tile.Wall && t != Tile.Fissure;
        }

        public void Tick(int playerX, int playerY) => Current?.Update(playerX, playerY);

        // The evacuation center is always safe.
        public bool IsDanger(int x, int y) => Current != null && GetTile(x, y) != Tile.Evac && Current.IsDanger(x, y);
        public bool IsWarning(int x, int y) => Current != null && GetTile(x, y) != Tile.Evac && Current.IsWarning(x, y);

        /// <summary>
        /// Turns the plain floor tiles of a crack into permanent fissures,
        /// unless that would make the player, the exit or a family member unreachable.
        /// </summary>
        public bool TryMakeFissure(List<Point> cells)
        {
            List<Point> candidates = cells.Where(p => GetTile(p.X, p.Y) == Tile.Floor).ToList();
            if (candidates.Count == 0 || FissureCount + candidates.Count > MaxFissures) return false;

            var blocked = new HashSet<Point>(candidates);
            List<Point> important = ImportantPoints?.Invoke()?.ToList() ?? new List<Point>();
            if (important.Any(blocked.Contains)) return false;
            if (!AllReachable(important, blocked)) return false;

            foreach (Point p in candidates) map[p.X, p.Y] = Tile.Fissure;
            FissureCount += candidates.Count;
            return true;
        }

        // Breadth-first search from the first important point.
        private bool AllReachable(List<Point> points, HashSet<Point> blocked)
        {
            if (points.Count == 0) return true;

            var seen = new HashSet<Point> { points[0] };
            var queue = new Queue<Point>();
            queue.Enqueue(points[0]);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    var next = new Point(p.X + dx[i], p.Y + dy[i]);
                    if (!IsWalkable(next.X, next.Y) || blocked.Contains(next) || !seen.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            return points.All(seen.Contains);
        }
    }
}
