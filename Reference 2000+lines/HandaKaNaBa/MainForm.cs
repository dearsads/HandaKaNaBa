// =====================================================================
//  MainForm.cs
//  OWNER: Member 4 - Front-End (Assets, Graphics & Compiler)
//
//  The ONE window of the game:
//    - Program.Main (the entry point) lives here, so the project stays at 5 files
//    - Sprites: loads the pixel art from Assets/ (.gif first, then .png)
//    - the 100 ms game loop
//    - keyboard input (WASD / arrows, 1-2-3 during cards)
//    - OnPaint: draws the 12x12 grid, hazards, family, player
//
//  Pixel art: draw at 16x16 and it is scaled 3x to 48x48 with
//  nearest-neighbor so it stays crisp. Animated GIFs play automatically.
// =====================================================================
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace HandaKaNaBa
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ------------------------------------------------------------------
    //  SPRITES: loads Assets/<name>.gif or Assets/<name>.png once.
    //  A missing file returns null, and the game draws a colored square.
    // ------------------------------------------------------------------
    public static class Sprites
    {
        private static readonly Dictionary<string, Image> cache = new Dictionary<string, Image>();
        private static readonly PrivateFontCollection fonts = new PrivateFontCollection();
        private static FontFamily display;

        public static string Folder => Path.Combine(AppContext.BaseDirectory, "Assets");

        public static Image Get(string name)
        {
            if (cache.TryGetValue(name, out Image cached)) return cached;

            Image image = null;
            foreach (string ext in new[] { ".gif", ".png" })
            {
                string path = Path.Combine(Folder, name + ext);
                if (!File.Exists(path)) continue;

                // Load from memory so the file is not locked while the game runs
                image = Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
                if (ImageAnimator.CanAnimate(image))
                    ImageAnimator.Animate(image, (s, e) => { });   // frames advance in OnPaint
                break;
            }

            cache[name] = image;
            return image;
        }

        /// <summary>The comic display font. Uses Assets/Bangers-Regular.ttf if present, else Impact.</summary>
        public static Font Display(float size)
        {
            if (display == null)
            {
                string ttf = Path.Combine(Folder, "Bangers-Regular.ttf");
                try
                {
                    if (File.Exists(ttf))
                    {
                        fonts.AddFontFile(ttf);
                        display = fonts.Families[0];
                    }
                    else
                    {
                        display = new FontFamily("Impact");
                    }
                }
                catch (ArgumentException)
                {
                    display = FontFamily.GenericSansSerif;
                }
            }
            return new Font(display, size, FontStyle.Regular);
        }
    }

    // ------------------------------------------------------------------
    //  THE WINDOW
    // ------------------------------------------------------------------
    public class MainForm : Form
    {
        public const int TileSize = 48;                  // 16x16 art scaled 3x
        public const int GridLeft = 16;
        public const int GridTop = 72;                   // below the 64 px HUD
        public const int GridPixels = TileSize * DisasterManager.Size;

        private readonly GameEngine engine = new GameEngine();
        private readonly UIManager ui;
        private readonly System.Windows.Forms.Timer gameLoop = new System.Windows.Forms.Timer { Interval = 100 };
        private int frame;

        public MainForm()
        {
            Text = "Handa Ka Na Ba? - Survival Grid";
            ClientSize = new Size(GridLeft * 2 + GridPixels, GridTop + GridPixels + 40);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(38, 38, 46);
            DoubleBuffered = true;
            KeyPreview = true;

            ui = new UIManager(this, engine);

            gameLoop.Tick += GameLoop_Tick;
            gameLoop.Start();
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            frame++;
            engine.Tick();
            ui.Tick();
            Invalidate();
        }

        // ProcessCmdKey sees every key first, even when a button has focus,
        // so the arrow keys are never "eaten" by the panels.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (engine.State == GameState.Walking)
            {
                switch (keyData)
                {
                    case Keys.W: case Keys.Up: engine.MovePlayer(0, -1); Invalidate(); return true;
                    case Keys.S: case Keys.Down: engine.MovePlayer(0, 1); Invalidate(); return true;
                    case Keys.A: case Keys.Left: engine.MovePlayer(-1, 0); Invalidate(); return true;
                    case Keys.D: case Keys.Right: engine.MovePlayer(1, 0); Invalidate(); return true;
                }
            }
            else if (engine.State == GameState.Encounter && ui.HandleEncounterKey(keyData))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ===============================================================
        //  DRAWING
        // ===============================================================
        private static Rectangle TileRect(int x, int y) =>
            new Rectangle(GridLeft + x * TileSize, GridTop + y * TileSize, TileSize, TileSize);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (engine.Player == null || engine.State == GameState.Menu) return;

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            ImageAnimator.UpdateFrames();

            var grid = new Rectangle(GridLeft, GridTop, GridPixels, GridPixels);
            DisasterManager map = engine.Map;
            bool blinkOn = frame % 4 < 2;

            // 1. Terrain
            for (int y = 0; y < DisasterManager.Size; y++)
            {
                for (int x = 0; x < DisasterManager.Size; x++)
                {
                    Rectangle r = TileRect(x, y);
                    switch (map.GetTile(x, y))
                    {
                        case Tile.Wall: DrawSprite(g, "wall", r, Color.FromArgb(150, 90, 60), "#"); break;
                        case Tile.High: DrawSprite(g, "high", r, Color.FromArgb(110, 200, 110), "^"); break;
                        case Tile.Fissure: DrawSprite(g, "fissure", r, Color.FromArgb(50, 40, 35), "X"); break;
                        case Tile.Evac: DrawEvac(g, r); break;
                        default: DrawSprite(g, "floor", r, UIManager.Cream, ""); break;
                    }
                }
            }

            // 2. Hazards: flashing warning, then the danger sprite
            string danger = map.Current?.DangerSprite ?? "water";
            for (int y = 0; y < DisasterManager.Size; y++)
            {
                for (int x = 0; x < DisasterManager.Size; x++)
                {
                    Rectangle r = TileRect(x, y);
                    if (map.IsDanger(x, y))
                    {
                        DrawSprite(g, danger, r, Color.FromArgb(200, 230, 60, 30), "!");
                    }
                    else if (map.IsWarning(x, y) && blinkOn)
                    {
                        using (var warn = new SolidBrush(Color.FromArgb(120, 255, 60, 60)))
                            g.FillRectangle(warn, r);
                        using (var pen = new Pen(UIManager.Red, 2))
                            g.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 5, r.Height - 5);
                    }
                }
            }

            // 3. Go-Bag
            if (engine.GoBag != null && !engine.GoBag.Taken)
                DrawSprite(g, engine.GoBag.Sprite, TileRect(engine.GoBag.X, engine.GoBag.Y), UIManager.Orange, "G");

            // 4. Family: waiting ones (with "!" when in danger), then followers
            foreach (FamilyMember f in engine.Family)
            {
                if (f.IsLost) continue;                                   // gone for good
                Rectangle r = TileRect(f.X, f.Y);
                DrawSprite(g, f.Sprite, r, UIManager.Pink, f.Name.Substring(0, 1));

                if (f.NeedsRescue)
                {
                    // safety pips: how many more hits they can take
                    for (int i = 0; i < FamilyMember.MaxSafety; i++)
                    {
                        var pip = new Rectangle(r.X + 4 + i * 9, r.Bottom - 9, 7, 7);
                        g.FillRectangle(new SolidBrush(i < f.Safety ? UIManager.Green : Color.Gray), pip);
                        g.DrawRectangle(Pens.Black, pip);
                    }
                    if (f.IsWarned && blinkOn) DrawBubble(g, r, "!", UIManager.Yellow);
                }
            }

            // 5. Player (blinks while invincible, cyan ring with a shield)
            if (!engine.Player.IsInvincible || blinkOn)
            {
                Rectangle pr = TileRect(engine.Player.X, engine.Player.Y);
                if (engine.Player.HasShield)
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var ring = new Pen(UIManager.Cyan, 3))
                        g.DrawEllipse(ring, pr.X + 1, pr.Y + 1, pr.Width - 3, pr.Height - 3);
                    g.SmoothingMode = SmoothingMode.None;
                }
                DrawSprite(g, engine.Player.Sprite, pr, UIManager.Pink, "IKAW");
            }

            // 6. Grid frame with a pop-art hard shadow
            using (var pen = new Pen(UIManager.Ink, 4))
                g.DrawRectangle(pen, grid.X - 2, grid.Y - 2, grid.Width + 3, grid.Height + 3);

            // 7. Banner and popups (UIManager)
            ui.DrawOverlay(g, grid);

            // 8. Hint line under the grid
            string hint = engine.ExitOpen ? "Bukas na ang EVAC! Tumakbo papunta roon."
                                          : engine.Level?.Tagline ?? "";
            using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
                TextRenderer.DrawText(g, hint, f, new Rectangle(0, grid.Bottom + 8, ClientSize.Width, 24),
                    UIManager.Yellow, TextFormatFlags.HorizontalCenter);
        }

        private void DrawEvac(Graphics g, Rectangle r)
        {
            DrawSprite(g, "evac", r, UIManager.Green, "EVAC");
            if (!engine.ExitOpen)
            {
                using (var shade = new SolidBrush(Color.FromArgb(150, 60, 60, 60)))
                    g.FillRectangle(shade, r);
                using (var f = new Font("Segoe UI", 7, FontStyle.Bold))
                    TextRenderer.DrawText(g, "SARADO", f, r, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else if (frame % 6 < 3)
            {
                using (var pen = new Pen(UIManager.Yellow, 4))
                    g.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 5, r.Height - 5);
            }
        }

        /// <summary>Draws a sprite, or a colored square with a letter if the file is missing.</summary>
        private static void DrawSprite(Graphics g, string name, Rectangle r, Color fallback, string letter)
        {
            Image img = Sprites.Get(name);
            if (img != null)
            {
                g.DrawImage(img, r);
                return;
            }
            using (var b = new SolidBrush(fallback))
                g.FillRectangle(b, r);
            if (letter.Length > 0)
                using (var f = new Font("Segoe UI", 8, FontStyle.Bold))
                    TextRenderer.DrawText(g, letter, f, r, UIManager.Ink,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static void DrawBubble(Graphics g, Rectangle tile, string text, Color color)
        {
            var bubble = new Rectangle(tile.Right - 18, tile.Y - 6, 20, 20);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(color)) g.FillEllipse(b, bubble);
            using (var pen = new Pen(UIManager.Ink, 2)) g.DrawEllipse(pen, bubble);
            using (var f = new Font("Segoe UI", 9, FontStyle.Bold))
                TextRenderer.DrawText(g, text, f, bubble, UIManager.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            g.SmoothingMode = SmoothingMode.None;
        }
    }
}
