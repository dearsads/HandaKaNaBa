// MainForm.cs - Member 2 (Drawing, input, assets, and visual presentation)
// Ang nag-iisang window: dito ang game loop, keyboard at pag-drawing ng grid.
// 16x16 ang pixel art, pinapalaki ng 3x (48x48) gamit ang NearestNeighbor para malinaw pa rin.
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace HandaKaNaBa
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private const int TILE = 48;
        private const int LEFT = 16;
        private const int TOP = 72;   // sa ilalim ng HUD

        private GameEngine engine = new GameEngine();
        private UIManager ui;
        private System.Windows.Forms.Timer gameLoop = new System.Windows.Forms.Timer();
        private Dictionary<string, Image> sprites = new Dictionary<string, Image>();
        private Font hintFont = new Font("Segoe UI", 10, FontStyle.Bold);
        private int frame;

        // Presentation-only movement tween. GameEngine still owns the true grid position.
        private const int PLAYER_MOVE_DURATION_MS = 100;
        private readonly System.Windows.Forms.Timer playerAnimationTimer =
            new System.Windows.Forms.Timer { Interval = 16 };
        private readonly Stopwatch playerAnimationClock = new Stopwatch();
        // Presentation-only environment animation. It never advances disaster gameplay.
        private readonly System.Windows.Forms.Timer environmentAnimationTimer =
            new System.Windows.Forms.Timer { Interval = 50 };
        private int environmentFrame;
        private readonly Dictionary<Point, int> houseCollapseEffects = new Dictionary<Point, int>();
        private readonly Brush waterRippleBrush = new SolidBrush(Color.FromArgb(150, 8, 145, 178));
        private readonly Brush waterFoamBrush = new SolidBrush(Color.FromArgb(205, 207, 250, 254));
        private readonly Brush tsunamiWarningWashBrush = new SolidBrush(Color.FromArgb(75, 34, 211, 238));
        private readonly Pen tsunamiWarningPen = new Pen(Color.FromArgb(190, 207, 250, 254), 2f);
        private readonly Brush shelterBadgeBrush = new SolidBrush(Color.FromArgb(245, 22, 163, 74));
        private readonly Pen shelterBadgeOutlinePen = new Pen(Color.FromArgb(245, 240, 253, 244), 1.2f);
        private readonly Pen shelterCheckPen = new Pen(Color.White, 1.5f);
        private readonly Brush quakeDangerWashBrush = new SolidBrush(Color.FromArgb(52, 239, 68, 68));
        private readonly Pen heatShimmerPen = new Pen(Color.FromArgb(150, 255, 233, 164), 2f);
        private readonly Font houseDamageFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        private PointF playerVisualPosition;
        private PointF playerAnimationStart;
        private PointF playerAnimationTarget;
        private bool playerVisualInitialized;
        private bool playerAnimating;
        private bool processingPlayerInput;

        public MainForm()
        {
            Text = "Handa Ka Na Ba?";
            ClientSize = new Size(LEFT * 2 + TILE * 12, TOP + TILE * 12 + 40);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(38, 38, 46);
            DoubleBuffered = true;   // walang flicker

            LoadSprites();
            ui = new UIManager(this, engine);

            // Keep the game simulation at 100 ms while rendering movement at roughly 60 FPS.
            playerAnimationTimer.Tick += PlayerAnimationTimer_Tick;
            environmentAnimationTimer.Tick += EnvironmentalAnimationTimer_Tick;
            engine.Map.HouseCollapsed += Engine_HouseCollapsed;
            engine.Changed += Engine_Changed;
            engine.EncounterStarted += Engine_EncounterStarted;
            engine.Ended += Engine_Ended;

            gameLoop.Interval = 100;   // 1 game tick = 100 ms
            gameLoop.Tick += GameLoop_Tick;
            gameLoop.Start();

            FormClosed += MainForm_FormClosed;
        }

        // load lahat ng .png sa Assets folder, ang pangalan ng file ang key
        private void LoadSprites()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "Assets");
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "*.png"))
                sprites[Path.GetFileNameWithoutExtension(file)] = Image.FromFile(file);
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            frame++;
            engine.Tick();
            ui.Tick();
            UpdateEnvironmentalAnimationState();
            Invalidate();
        }

        // Snap after level resets, encounter resolution, or game completion.
        // Ordinary movement is animated separately without changing simulation timing.
        private void Engine_Changed()
        {
            if (engine.Ticks == 0 && engine.State == GameState.Walking)
            {
                environmentFrame = 0;
                houseCollapseEffects.Clear();
            }

            UpdateEnvironmentalAnimationState();

            if (engine.Player == null || processingPlayerInput)
                return;

            // Ticks == 0 identifies a fresh level; non-walking states include encounter feedback.
            if (engine.State != GameState.Walking || engine.Ticks == 0)
                SnapPlayerVisualToEngine();
        }

        private void Engine_EncounterStarted()
        {
            UpdateEnvironmentalAnimationState();
        }

        private void Engine_Ended()
        {
            UpdateEnvironmentalAnimationState();
            if (engine.Player != null)
                SnapPlayerVisualToEngine();
        }

        private void UpdateEnvironmentalAnimationState()
        {
            bool shouldAnimate = engine.Player != null && engine.State == GameState.Walking;
            if (shouldAnimate && !environmentAnimationTimer.Enabled)
                environmentAnimationTimer.Start();
            else if (!shouldAnimate && environmentAnimationTimer.Enabled)
                environmentAnimationTimer.Stop();
        }

        private void EnvironmentalAnimationTimer_Tick(object sender, EventArgs e)
        {
            if (engine.State != GameState.Walking || engine.Player == null)
            {
                environmentAnimationTimer.Stop();
                return;
            }

            environmentFrame = (environmentFrame + 1) % 120;
            foreach (Point cell in houseCollapseEffects.Keys.ToList())
            {
                houseCollapseEffects[cell]++;
                if (houseCollapseEffects[cell] > 14)
                    houseCollapseEffects.Remove(cell);
            }
            Invalidate();
        }

        private void Engine_HouseCollapsed(int x, int y)
        {
            houseCollapseEffects[new Point(x, y)] = 0;
            Invalidate();
        }

        private void PlayerAnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!playerAnimating)
            {
                playerAnimationTimer.Stop();
                return;
            }

            if (playerAnimationClock.ElapsedMilliseconds >= PLAYER_MOVE_DURATION_MS)
            {
                playerVisualPosition = playerAnimationTarget;
                playerAnimating = false;
                playerAnimationClock.Stop();
                playerAnimationTimer.Stop();
            }

            Invalidate();
        }

        private void StartPlayerMoveAnimation()
        {
            if (engine.Player == null)
                return;

            if (!playerVisualInitialized)
                SnapPlayerVisualToEngine();

            // Continue smoothly from the current rendered point if another key arrives mid-step.
            playerAnimationStart = GetPlayerAnimationPosition(out _);
            playerAnimationTarget = new PointF(engine.Player.X, engine.Player.Y);

            if (Math.Abs(playerAnimationTarget.X - playerAnimationStart.X) < 0.001f &&
                Math.Abs(playerAnimationTarget.Y - playerAnimationStart.Y) < 0.001f)
            {
                SnapPlayerVisualToEngine();
                return;
            }

            playerAnimationClock.Restart();
            playerAnimating = true;
            playerAnimationTimer.Start();
        }

        private PointF GetPlayerAnimationPosition(out float progress)
        {
            if (!playerAnimating)
            {
                progress = 0f;
                return playerVisualPosition;
            }

            float rawProgress = Math.Clamp(
                (float)(playerAnimationClock.Elapsed.TotalMilliseconds / PLAYER_MOVE_DURATION_MS),
                0f,
                1f);

            // Ease out so the character settles gently on the destination tile.
            progress = rawProgress;
            float eased = 1f - MathF.Pow(1f - rawProgress, 3f);

            return new PointF(
                playerAnimationStart.X + (playerAnimationTarget.X - playerAnimationStart.X) * eased,
                playerAnimationStart.Y + (playerAnimationTarget.Y - playerAnimationStart.Y) * eased);
        }

        private void SnapPlayerVisualToEngine()
        {
            if (engine.Player == null)
                return;

            playerAnimationTimer.Stop();
            playerAnimationClock.Stop();
            playerAnimating = false;
            playerVisualPosition = new PointF(engine.Player.X, engine.Player.Y);
            playerAnimationStart = playerVisualPosition;
            playerAnimationTarget = playerVisualPosition;
            playerVisualInitialized = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            gameLoop.Stop();
            playerAnimationTimer.Stop();
            environmentAnimationTimer.Stop();
            playerAnimationClock.Stop();
            engine.Map.HouseCollapsed -= Engine_HouseCollapsed;
            engine.Changed -= Engine_Changed;
            engine.EncounterStarted -= Engine_EncounterStarted;
            engine.Ended -= Engine_Ended;
            ui?.Dispose();

            foreach (Image sprite in sprites.Values)
                sprite.Dispose();

            hintFont.Dispose();
            gameLoop.Dispose();
            playerAnimationTimer.Dispose();
            environmentAnimationTimer.Dispose();
            waterRippleBrush.Dispose();
            waterFoamBrush.Dispose();
            tsunamiWarningWashBrush.Dispose();
            tsunamiWarningPen.Dispose();
            shelterBadgeBrush.Dispose();
            shelterBadgeOutlinePen.Dispose();
            shelterCheckPen.Dispose();
            quakeDangerWashBrush.Dispose();
            heatShimmerPen.Dispose();
            houseDamageFont.Dispose();
        }

        // ProcessCmdKey: nakikita nito ang lahat ng key kahit may button na naka-focus
        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if (engine.State == GameState.Walking)
            {
                int dx, dy;
                if (key == Keys.W || key == Keys.Up) { dx = 0; dy = -1; }
                else if (key == Keys.S || key == Keys.Down) { dx = 0; dy = 1; }
                else if (key == Keys.A || key == Keys.Left) { dx = -1; dy = 0; }
                else if (key == Keys.D || key == Keys.Right) { dx = 1; dy = 0; }
                else return base.ProcessCmdKey(ref msg, key);

                if (engine.Player == null)
                    return base.ProcessCmdKey(ref msg, key);

                // Capture the origin before the engine updates its logical grid position.
                if (!playerVisualInitialized)
                {
                    playerVisualPosition = new PointF(engine.Player.X, engine.Player.Y);
                    playerAnimationStart = playerVisualPosition;
                    playerAnimationTarget = playerVisualPosition;
                    playerVisualInitialized = true;
                }

                int oldX = engine.Player.X;
                int oldY = engine.Player.Y;

                processingPlayerInput = true;
                try
                {
                    engine.MovePlayer(dx, dy);
                }
                finally
                {
                    processingPlayerInput = false;
                }

                bool moved = engine.Player.X != oldX || engine.Player.Y != oldY;
                if (moved && engine.State == GameState.Walking)
                    StartPlayerMoveAnimation();
                else if (engine.State != GameState.Walking)
                    SnapPlayerVisualToEngine();

                Invalidate();
                return true;
            }

            if (engine.State == GameState.Encounter && ui.HandleKey(key))
                return true;

            return base.ProcessCmdKey(ref msg, key);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (engine.Player == null) return;

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            bool blink = frame % 4 < 2;
            bool elNino = engine.Disaster == Difficulty.ElNino;
            GraphicsState worldState = g.Save();
            Point shake = GetEarthquakeShakeOffset();
            if (shake.X != 0 || shake.Y != 0)
                g.TranslateTransform(shake.X, shake.Y);

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    Rectangle r = TileRect(x, y);
                    Tile t = engine.Map.GetTile(x, y);

                    // 1. Terrain. Damaged houses crack; collapsed houses become walkable rubble.
                    if (t == Tile.Wall)
                    {
                        Draw(g, "wall", r);
                        if (engine.Disaster == Difficulty.Tsunami)
                            DrawDamagedHouse(g, r, engine.Map.GetHouseDamage(x, y));
                    }
                    else if (t == Tile.High) Draw(g, "high", r);
                    else if (t == Tile.Evac) DrawEvac(g, r, blink);
                    else
                    {
                        Draw(g, elNino ? "grass" : "floor", r);
                        if (engine.Map.WasHouseDestroyed(x, y))
                            DrawCollapsedHouseRubble(g, r);
                    }

                    // 2. Disaster warnings/danger. Only the presentation is animated here.
                    if (engine.Map.IsDanger(x, y))
                    {
                        if (engine.Disaster == Difficulty.Tsunami)
                            DrawAnimatedWater(g, r);
                        else if (engine.Disaster == Difficulty.Lindol)
                            DrawEarthquakeDanger(g, r);
                        else
                        {
                            Draw(g, engine.Map.Current.DangerSprite, r);
                            DrawHeatShimmer(g, r);
                        }
                    }
                    else if (engine.Map.IsWarning(x, y))
                    {
                        if (engine.Disaster == Difficulty.Tsunami)
                            DrawTsunamiWarning(g, r);
                        else if (engine.Disaster == Difficulty.Lindol)
                            DrawEarthquakeWarning(g, r);
                        else
                            DrawElNinoWarning(g, r);
                    }

                    if (houseCollapseEffects.TryGetValue(new Point(x, y), out int collapseAge))
                        DrawHouseCollapseEffect(g, r, collapseAge);
                }
            }

            // 3. pamilyang hindi pa nasasagip
            foreach (FamilyMember f in engine.Family)
                if (!f.Rescued) Draw(g, f.Sprite, TileRect(f.X, f.Y));

            // 4. player: smoothly interpolated between logical tiles; still blinks after damage.
            if (engine.Player.Invincible == 0 || blink)
            {
                if (!playerVisualInitialized)
                    SnapPlayerVisualToEngine();

                PointF visualPosition = GetPlayerAnimationPosition(out float moveProgress);
                int playerX = LEFT + (int)Math.Round(visualPosition.X * TILE);
                int playerY = TOP + (int)Math.Round(visualPosition.Y * TILE);

                // A tiny hop gives the existing single-frame sprite a sense of footfall.
                int stepBob = playerAnimating
                    ? (int)Math.Round(Math.Sin(moveProgress * Math.PI) * 2.0)
                    : 0;

                var playerRect = new Rectangle(playerX, playerY - stepBob, TILE, TILE);
                Draw(g, engine.Player.Sprite, playerRect);

                if (engine.Disaster == Difficulty.Tsunami &&
                    engine.Map.IsTsunamiShelteredByTerrain(engine.Player.X, engine.Player.Y))
                    DrawShelterBadge(g, playerRect);
            }

            // 5. makapal na border at contextual hint sa ilalim
            using (var border = new Pen(UIManager.Ink, 4))
                g.DrawRectangle(border, LEFT - 2, TOP - 2, TILE * 12 + 3, TILE * 12 + 3);

            string hint;
            if (engine.ExitOpen)
                hint = "Bukas na ang EVAC! Takbo!";
            else if (engine.Disaster == Difficulty.Tsunami)
            {
                Tile playerTile = engine.Map.GetTile(engine.Player.X, engine.Player.Y);
                if (playerTile == Tile.High)
                    hint = "MATAAS NA LUGAR: LIGTAS KA SA TSUNAMI!";
                else if (engine.Map.IsTsunamiShelteredByTerrain(engine.Player.X, engine.Player.Y))
                    hint = "NAKASILONG KA! Hinarang ng pader/burol ang alon.";
                else
                    hint = "TSUNAMI: SUMILONG 1-3 TILES SA LIKOD NG PADER/BUROL SA HILERA MO!";
            }
            else
                hint = "Sagipin sina Lola, Bunso at Bantay. Iwasan ang kumikislap!";

            TextRenderer.DrawText(g, hint, hintFont, new Rectangle(0, TOP + TILE * 12 + 8, ClientSize.Width, 24),
                UIManager.Yellow, TextFormatFlags.HorizontalCenter);
            g.Restore(worldState);
        }


        // A compact shield badge makes the terrain-cover state visible without changing gameplay.
        private void DrawShelterBadge(Graphics g, Rectangle playerRect)
        {
            int x = playerRect.Right - 16;
            int y = playerRect.Top + 2;
            Point[] shield =
            {
                new Point(x + 2, y + 1),
                new Point(x + 13, y + 1),
                new Point(x + 12, y + 8),
                new Point(x + 7, y + 14),
                new Point(x + 3, y + 8)
            };

            g.FillPolygon(shelterBadgeBrush, shield);
            g.DrawPolygon(shelterBadgeOutlinePen, shield);
            g.DrawLines(shelterCheckPen, new[]
            {
                new Point(x + 4, y + 7),
                new Point(x + 6, y + 9),
                new Point(x + 10, y + 4)
            });
        }

        // Draw animated surface details over the existing water sprite.
        // The hazard's actual position and damage checks remain entirely in DisasterManager.
        private void DrawAnimatedWater(Graphics g, Rectangle r)
        {
            Draw(g, "water", r);

            GraphicsState state = g.Save();
            try
            {
                g.SetClip(r, CombineMode.Intersect);

                int travel = (environmentFrame * 2 + r.X / 3 + r.Y / 6) % (TILE + 16);
                int firstX = r.X + travel - 16;
                int secondX = r.X + ((travel + 25) % (TILE + 16)) - 16;
                int rowShift = (environmentFrame / 5 + r.X / TILE) % 3;

                g.FillRectangle(waterRippleBrush, firstX, r.Y + 10 + rowShift * 3, 16, 3);
                g.FillRectangle(waterFoamBrush, firstX + 3, r.Y + 8 + rowShift * 3, 8, 2);

                g.FillRectangle(waterRippleBrush, secondX, r.Y + 29 - rowShift * 2, 13, 3);
                g.FillRectangle(waterFoamBrush, secondX + 2, r.Y + 27 - rowShift * 2, 6, 2);

                // Small moving highlights add a pixel-art shimmer without new sprite assets.
                int glintX = r.X + ((environmentFrame * 3 + r.Y / 2) % (TILE - 6));
                g.FillRectangle(waterFoamBrush, glintX, r.Y + 19, 3, 3);
            }
            finally
            {
                g.Restore(state);
            }
        }

        // The existing warning tiles stay exactly where the hazard system places them.
        // Only their presentation changes: a cyan pulse and a moving foam marker signal the wave.
        private void DrawTsunamiWarning(Graphics g, Rectangle r)
        {
            GraphicsState state = g.Save();
            try
            {
                g.SetClip(r, CombineMode.Intersect);
                g.FillRectangle(tsunamiWarningWashBrush, r);

                int travel = (environmentFrame * 2) % (TILE + 12);
                int foamX = r.X + travel - 12;
                int secondaryX = r.X + ((travel + 24) % (TILE + 12)) - 12;
                g.FillRectangle(waterFoamBrush, foamX, r.Y + 13, 12, 3);
                g.FillRectangle(waterFoamBrush, secondaryX, r.Y + 30, 8, 2);

                if ((environmentFrame / 3) % 2 == 0)
                    g.DrawRectangle(tsunamiWarningPen, r.X + 2, r.Y + 2, r.Width - 5, r.Height - 5);
            }
            finally
            {
                g.Restore(state);
            }
        }

        private Point GetEarthquakeShakeOffset()
        {
            if (engine.Disaster != Difficulty.Lindol || engine.State != GameState.Walking || engine.Player == null)
                return Point.Empty;

            bool warningNearby = false;
            for (int y = engine.Player.Y - 1; y <= engine.Player.Y + 1 && !warningNearby; y++)
            {
                for (int x = engine.Player.X - 1; x <= engine.Player.X + 1; x++)
                {
                    if (engine.Map.IsWarning(x, y))
                    {
                        warningNearby = true;
                        break;
                    }
                }
            }

            if (!warningNearby)
                return Point.Empty;

            int offset = environmentFrame % 4 < 2 ? -2 : 2;
            return new Point(offset, environmentFrame % 6 < 3 ? 1 : -1);
        }

        private void DrawDamagedHouse(Graphics g, Rectangle r, int damage)
        {
            if (damage <= 0)
                return;

            using (var crack = new Pen(Color.FromArgb(235, 45, 35, 29), 2.4f))
            using (var highlight = new Pen(Color.FromArgb(190, 255, 231, 170), 1f))
            {
                Point[] mainCrack =
                {
                    new Point(r.X + 10, r.Y + 5),
                    new Point(r.X + 17, r.Y + 17),
                    new Point(r.X + 12, r.Y + 25),
                    new Point(r.X + 26, r.Y + 42)
                };
                g.DrawLines(highlight, new[]
                {
                    new Point(mainCrack[0].X + 1, mainCrack[0].Y),
                    new Point(mainCrack[1].X + 1, mainCrack[1].Y),
                    new Point(mainCrack[2].X + 1, mainCrack[2].Y),
                    new Point(mainCrack[3].X + 1, mainCrack[3].Y)
                });
                g.DrawLines(crack, mainCrack);
                g.DrawLines(crack, new[]
                {
                    new Point(r.X + 17, r.Y + 17),
                    new Point(r.X + 30, r.Y + 14),
                    new Point(r.X + 35, r.Y + 7)
                });
                if (damage >= 2)
                {
                    g.DrawLines(crack, new[]
                    {
                        new Point(r.X + 12, r.Y + 25),
                        new Point(r.X + 5, r.Y + 33),
                        new Point(r.X + 9, r.Y + 43)
                    });
                }
            }

            // Damage badge indicates that the house has one hit remaining (threshold = 2).
            using (var badge = new SolidBrush(Color.FromArgb(230, 255, 244, 214)))
            {
                g.FillRectangle(badge, r.X + 3, r.Y + 3, 17, 12);
                TextRenderer.DrawText(g,
                    damage + "/" + DisasterManager.TsunamiHouseHitThreshold,
                    houseDamageFont,
                    new Rectangle(r.X + 3, r.Y + 3, 17, 12), Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void DrawCollapsedHouseRubble(Graphics g, Rectangle r)
        {
            using (var rubble = new SolidBrush(Color.FromArgb(225, 112, 92, 74)))
            using (var rubbleLight = new SolidBrush(Color.FromArgb(230, 174, 151, 119)))
            using (var outline = new Pen(Color.FromArgb(220, 55, 47, 39), 1.3f))
            {
                g.FillRectangle(rubble, r.X + 5, r.Y + 31, 15, 9);
                g.FillRectangle(rubbleLight, r.X + 20, r.Y + 35, 13, 8);
                g.FillRectangle(rubble, r.X + 30, r.Y + 27, 11, 10);
                g.DrawRectangle(outline, r.X + 5, r.Y + 31, 15, 9);
                g.DrawRectangle(outline, r.X + 20, r.Y + 35, 13, 8);
                g.DrawRectangle(outline, r.X + 30, r.Y + 27, 11, 10);
            }
        }

        private void DrawHouseCollapseEffect(Graphics g, Rectangle r, int age)
        {
            float progress = Math.Clamp(age / 14f, 0f, 1f);
            int alpha = (int)(190 * (1f - progress));
            if (alpha <= 0) return;

            for (int i = 0; i < 7; i++)
            {
                double angle = (Math.PI * 2 * i / 7.0) - Math.PI / 2;
                float distance = 5f + progress * 23f;
                int px = r.X + TILE / 2 + (int)(Math.Cos(angle) * distance);
                int py = r.Y + TILE / 2 + (int)(Math.Sin(angle) * distance);
                int size = i % 3 == 0 ? 5 : 3;
                using (var dust = new SolidBrush(Color.FromArgb(alpha,
                    i % 2 == 0 ? 166 : 113, i % 2 == 0 ? 139 : 91, i % 2 == 0 ? 105 : 68)))
                    g.FillRectangle(dust, px, py, size, size);
            }

            using (var cloud = new SolidBrush(Color.FromArgb(alpha / 3, 225, 213, 190)))
                g.FillEllipse(cloud, r.X + 7 - (int)(progress * 4), r.Y + 10 - (int)(progress * 8),
                    34 + (int)(progress * 10), 22 + (int)(progress * 8));
        }

        private void DrawEarthquakeWarning(Graphics g, Rectangle r)
        {
            int pulse = 45 + (environmentFrame % 10) * 6;
            using (var wash = new SolidBrush(Color.FromArgb(Math.Clamp(pulse, 45, 105), 249, 115, 22)))
                g.FillRectangle(wash, r);

            using (var crack = new Pen(Color.FromArgb(210, 124, 45, 18), 2.1f))
            {
                g.DrawLines(crack, new[]
                {
                    new Point(r.X + 9, r.Y + 4), new Point(r.X + 17, r.Y + 18),
                    new Point(r.X + 13, r.Y + 25), new Point(r.X + 25, r.Y + 43)
                });
                g.DrawLines(crack, new[]
                {
                    new Point(r.X + 17, r.Y + 18), new Point(r.X + 30, r.Y + 12)
                });
            }
        }

        private void DrawEarthquakeDanger(Graphics g, Rectangle r)
        {
            Draw(g, "crack", r);
            if (environmentFrame % 4 < 2)
                g.FillRectangle(quakeDangerWashBrush, r);
        }

        private void DrawElNinoWarning(Graphics g, Rectangle r)
        {
            int alpha = 65 + (environmentFrame % 8) * 5;
            using (var wash = new SolidBrush(Color.FromArgb(Math.Clamp(alpha, 65, 100), 176, 125, 43)))
                g.FillRectangle(wash, r);

            int shimmerY = r.Y + 13 + (environmentFrame % 5) * 3;
            using (var shimmer = new Pen(Color.FromArgb(145, 255, 229, 161), 1.5f))
            {
                g.DrawLine(shimmer, r.X + 5, shimmerY, r.X + 18, shimmerY - 2);
                g.DrawLine(shimmer, r.X + 24, shimmerY + 4, r.X + 42, shimmerY + 1);
            }
        }

        private void DrawHeatShimmer(Graphics g, Rectangle r)
        {
            if (environmentFrame % 3 != 0) return;
            int y = r.Y + 8 + (environmentFrame % 7) * 4;
            g.DrawLine(heatShimmerPen, r.X + 5, y, r.X + 18, y - 2);
            g.DrawLine(heatShimmerPen, r.X + 25, y + 3, r.X + 43, y + 1);
        }

        private void DrawEvac(Graphics g, Rectangle r, bool blink)
        {
            Draw(g, "evac", r);
            if (!engine.ExitOpen)   // sarado pa: kulay abo
                using (var shade = new SolidBrush(Color.FromArgb(150, 60, 60, 60))) g.FillRectangle(shade, r);
            else if (blink)         // bukas na: kumikislap na dilaw
                using (var pen = new Pen(UIManager.Yellow, 4)) g.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 5, r.Height - 5);
        }

        private Rectangle TileRect(int x, int y) => new Rectangle(LEFT + x * TILE, TOP + y * TILE, TILE, TILE);

        // kapag walang picture, magenta na kahon para makita agad na may kulang
        private void Draw(Graphics g, string name, Rectangle r)
        {
            if (sprites.ContainsKey(name)) g.DrawImage(sprites[name], r);
            else g.FillRectangle(Brushes.Magenta, r);
        }
    }
}
