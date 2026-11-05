// DisasterManager.cs - Member 1 (Back-end: map and disaster mechanics)
// Ang 12x12 na mapa at ang tatlong sakuna. 1 game tick = 100 ms.
namespace HandaKaNaBa
{
    public enum Tile { Floor, Wall, High, Evac }

    // ABSTRACT parent: bawat sakuna ay may sariling update, warning, at danger behavior.
    public abstract class Hazard
    {
        protected DisasterManager map;
        protected Random rng = new Random();
        protected int age;

        protected Hazard(DisasterManager map) { this.map = map; }

        public abstract string Name { get; }
        public abstract string DangerSprite { get; }
        public abstract void Update(int playerX, int playerY);
        public abstract bool IsWarning(int x, int y);
        public abstract bool IsDanger(int x, int y);
    }

    // TSUNAMI: three-column wave moving from left to right.
    // Hills are safe; walls/houses shelter floor tiles behind them until they collapse.
    public class Tsunami : Hazard
    {
        private const int ShelterDepth = 3;
        private int front = -20;
        private int waves;
        private readonly HashSet<int> housesHitThisWave = new HashSet<int>();

        public Tsunami(DisasterManager map) : base(map) { }
        public override string Name => "TSUNAMI";
        public override string DangerSprite => "water";
        public int WavesCompleted => waves;

        public override void Update(int playerX, int playerY)
        {
            age++;
            int speed = waves >= 3 ? 2 : 3; // Faster after the third wave.

            if (age % speed == 0)
            {
                front++;

                // The leading edge damages each connected house once per wave.
                if (front >= 0 && front < 12)
                    map.ApplyTsunamiImpactColumn(front, housesHitThisWave);
            }

            if (front > 14)
            {
                waves++;
                front = -Math.Max(10, 20 - waves * 3);
                housesHitThisWave.Clear();
            }
        }

        // A floor tile is sheltered when a wall/house or high-ground tile sits
        // one to three cells to its left in the same row (the wave comes from left).
        public bool IsShelteredByTerrain(int x, int y)
        {
            if (map.GetTile(x, y) != Tile.Floor)
                return false;

            int firstCoverX = Math.Max(0, x - ShelterDepth);
            for (int coverX = x - 1; coverX >= firstCoverX; coverX--)
            {
                Tile cover = map.GetTile(coverX, y);
                if (cover == Tile.Wall || cover == Tile.High)
                    return true;
            }

            return false;
        }

        public override bool IsWarning(int x, int y)
        {
            if (map.GetTile(x, y) != Tile.Floor || IsShelteredByTerrain(x, y))
                return false;

            return front < 0 && front >= -6 ? x < 2 : x == front + 1;
        }

        public override bool IsDanger(int x, int y)
        {
            bool insideWave = front >= 0 && x <= front && x > front - 3;
            return insideWave && map.GetTile(x, y) == Tile.Floor && !IsShelteredByTerrain(x, y);
        }
    }

    // LINDOL: row/column cracks warn first, then become dangerous.
    public class Lindol : Hazard
    {
        private class Crack
        {
            public List<Point> Cells = new List<Point>();
            public int Born;
        }

        private readonly List<Crack> cracks = new List<Crack>();

        public Lindol(DisasterManager map) : base(map) { }
        public override string Name => "LINDOL";
        public override string DangerSprite => "crack";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(10, 30 - age / 60);
            if (age % every == 0) AddCrack();
            cracks.RemoveAll(c => age - c.Born > 27);
        }

        private void AddCrack()
        {
            int length = Math.Min(7, 3 + age / 150);
            bool horizontal = rng.Next(2) == 0;
            int line = rng.Next(12);
            int start = rng.Next(12 - length + 1);
            var crack = new Crack { Born = age };

            for (int i = 0; i < length; i++)
                crack.Cells.Add(horizontal
                    ? new Point(start + i, line)
                    : new Point(line, start + i));

            cracks.Add(crack);
        }

        public override bool IsWarning(int x, int y) =>
            cracks.Any(c => age - c.Born < 12 && c.Cells.Contains(new Point(x, y)));

        public override bool IsDanger(int x, int y) =>
            cracks.Any(c => age - c.Born >= 12 && c.Cells.Contains(new Point(x, y)));
    }

    // EL NIÑO: 3x3 patches transition from warning to dry, dangerous ground.
    public class ElNino : Hazard
    {
        private class Patch { public int X, Y, Born; }
        private readonly List<Patch> patches = new List<Patch>();

        public ElNino(DisasterManager map) : base(map) { }
        public override string Name => "EL NIÑO";
        public override string DangerSprite => "dry";

        public override void Update(int playerX, int playerY)
        {
            age++;
            int every = Math.Max(12, 28 - age / 75);
            int count = Math.Min(3, 1 + age / 300);

            for (int i = 0; age % every == 0 && i < count; i++)
            {
                bool nearPlayer = rng.Next(100) < 60;
                int x = nearPlayer ? playerX + rng.Next(-3, 4) : rng.Next(12);
                int y = nearPlayer ? playerY + rng.Next(-3, 4) : rng.Next(12);
                patches.Add(new Patch
                {
                    X = Math.Clamp(x, 0, 11),
                    Y = Math.Clamp(y, 0, 11),
                    Born = age
                });
            }

            patches.RemoveAll(p => age - p.Born > 32);
        }

        private bool InPatch(Patch p, int x, int y) =>
            Math.Abs(p.X - x) <= 1 && Math.Abs(p.Y - y) <= 1;

        public override bool IsWarning(int x, int y) =>
            patches.Any(p => age - p.Born < 18 && InPatch(p, x, y));

        public override bool IsDanger(int x, int y) =>
            patches.Any(p => age - p.Born >= 18 && InPatch(p, x, y));
    }

    public class DisasterManager
    {
        public const int TsunamiHouseHitThreshold = 2;
        private const int MapSize = 12;

        private Tile[,] tiles = new Tile[MapSize, MapSize];
        private int[,] houseIds = new int[MapSize, MapSize];
        private bool[,] destroyedHouseTiles = new bool[MapSize, MapSize];
        private readonly Dictionary<int, List<Point>> houseCells = new Dictionary<int, List<Point>>();
        private readonly Dictionary<int, int> houseDamage = new Dictionary<int, int>();
        private int nextHouseId;

        public Hazard Current { get; private set; }
        public Point Start { get; private set; }
        public Point Exit { get; private set; }
        public Dictionary<FamilyType, Point> FamilySpots { get; } = new Dictionary<FamilyType, Point>();

        // MainForm listens for this event to play the visual collapse effect.
        public event Action<int, int> HouseCollapsed;

        public void Load(Difficulty difficulty)
        {
            string[] rows = EncounterDatabase.GetMap(difficulty);
            tiles = new Tile[MapSize, MapSize];
            houseIds = new int[MapSize, MapSize];
            destroyedHouseTiles = new bool[MapSize, MapSize];
            houseCells.Clear();
            houseDamage.Clear();
            FamilySpots.Clear();
            nextHouseId = 0;

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    houseIds[x, y] = -1;
                    char symbol = rows[y][x];
                    Tile tile = Tile.Floor;

                    if (symbol == '#') tile = Tile.Wall;
                    else if (symbol == '^') tile = Tile.High;
                    else if (symbol == 'E') { tile = Tile.Evac; Exit = new Point(x, y); }
                    else if (symbol == 'P') { tile = Tile.High; Start = new Point(x, y); }
                    else if (symbol == 'L') { tile = Tile.High; FamilySpots[FamilyType.Lola] = new Point(x, y); }
                    else if (symbol == 'B') { tile = Tile.High; FamilySpots[FamilyType.Bunso] = new Point(x, y); }
                    else if (symbol == 'D') { tile = Tile.High; FamilySpots[FamilyType.Bantay] = new Point(x, y); }

                    tiles[x, y] = tile;
                }
            }

            BuildConnectedHouses();

            if (difficulty == Difficulty.Tsunami) Current = new Tsunami(this);
            else if (difficulty == Difficulty.Lindol) Current = new Lindol(this);
            else Current = new ElNino(this);
        }

        // Treat orthogonally connected '#' map cells as one house/building.
        private void BuildConnectedHouses()
        {
            bool[,] visited = new bool[MapSize, MapSize];
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    if (tiles[x, y] != Tile.Wall || visited[x, y])
                        continue;

                    int houseId = nextHouseId++;
                    var cells = new List<Point>();
                    var queue = new Queue<Point>();
                    queue.Enqueue(new Point(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        Point current = queue.Dequeue();
                        cells.Add(current);
                        houseIds[current.X, current.Y] = houseId;

                        for (int direction = 0; direction < 4; direction++)
                        {
                            int nx = current.X + dx[direction];
                            int ny = current.Y + dy[direction];

                            if (nx < 0 || nx >= MapSize || ny < 0 || ny >= MapSize)
                                continue;
                            if (visited[nx, ny] || tiles[nx, ny] != Tile.Wall)
                                continue;

                            visited[nx, ny] = true;
                            queue.Enqueue(new Point(nx, ny));
                        }
                    }

                    houseCells[houseId] = cells;
                    houseDamage[houseId] = 0;
                }
            }
        }

        // Called by the tsunami's leading edge. One connected house can only take
        // one hit per wave, even when the wave passes several of its wall tiles.
        internal void ApplyTsunamiImpactColumn(int x, HashSet<int> housesHitThisWave)
        {
            if (x < 0 || x >= MapSize || housesHitThisWave == null)
                return;

            for (int y = 0; y < MapSize; y++)
            {
                if (tiles[x, y] != Tile.Wall)
                    continue;

                int houseId = houseIds[x, y];
                if (houseId < 0 || !housesHitThisWave.Add(houseId))
                    continue;

                int damage = houseDamage[houseId] + 1;
                houseDamage[houseId] = damage;

                if (damage < TsunamiHouseHitThreshold)
                    continue;

                // The full connected structure collapses together and becomes rubble/floor.
                foreach (Point cell in houseCells[houseId])
                {
                    tiles[cell.X, cell.Y] = Tile.Floor;
                    destroyedHouseTiles[cell.X, cell.Y] = true;
                    HouseCollapsed?.Invoke(cell.X, cell.Y);
                }
            }
        }

        public int GetHouseDamage(int x, int y)
        {
            if (!IsInsideMap(x, y)) return 0;
            int id = houseIds[x, y];
            return id >= 0 && houseDamage.TryGetValue(id, out int damage) ? damage : 0;
        }

        public bool WasHouseDestroyed(int x, int y) =>
            IsInsideMap(x, y) && destroyedHouseTiles[x, y];

        private static bool IsInsideMap(int x, int y) =>
            x >= 0 && x < MapSize && y >= 0 && y < MapSize;

        // Outside the map counts as a wall.
        public Tile GetTile(int x, int y) =>
            !IsInsideMap(x, y) ? Tile.Wall : tiles[x, y];

        public bool IsWalkable(int x, int y) => GetTile(x, y) != Tile.Wall;
        public void Tick(int playerX, int playerY) => Current.Update(playerX, playerY);

        public bool IsTsunamiShelteredByTerrain(int x, int y) =>
            Current is Tsunami tsunami && tsunami.IsShelteredByTerrain(x, y);

        // The evacuation center is always safe.
        public bool IsDanger(int x, int y) =>
            GetTile(x, y) != Tile.Evac && Current.IsDanger(x, y);

        public bool IsWarning(int x, int y) =>
            GetTile(x, y) != Tile.Evac && Current.IsWarning(x, y);
    }
}
