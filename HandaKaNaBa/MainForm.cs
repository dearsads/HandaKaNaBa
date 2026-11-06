// MainForm.cs - Member 4 (Assets, Graphics & Compiler)
// Ang nag-iisang window: dito ang game loop, keyboard at pag-drawing ng grid.
// 16x16 ang pixel art, pinapalaki ng 3x (48x48) gamit ang NearestNeighbor para malinaw pa rin.
using System.Drawing.Drawing2D;

namespace HandaKaNaBa
{
    static class Program // Static means this startup helper is used directly, without creating a Program object.
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); // Start the Windows Forms message loop and show the main window.
        }
    }

    public class MainForm : Form // Inherits the standard Windows window behavior from Form.
    {
        private const int TILE = 48;
        private const int LEFT = 16;
        private const int TOP = 72;   // sa ilalim ng HUD

        private GameEngine engine = new GameEngine(); // MainForm asks the engine for rules; it does not calculate the rules itself.
        private UIManager ui;
        private System.Windows.Forms.Timer gameLoop = new System.Windows.Forms.Timer(); // Timer sends a Tick event repeatedly while running.
        private Dictionary<string, Image> sprites = new Dictionary<string, Image>();
        private Font hintFont = new Font("Segoe UI", 10, FontStyle.Bold);
        private int frame;

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

            gameLoop.Interval = 100;   // 1 tick = 100 ms
            gameLoop.Tick += GameLoop_Tick; // Subscribe: call this method each time the timer raises Tick.
            gameLoop.Start();
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
            Invalidate();
        }

        // ProcessCmdKey: nakikita nito ang lahat ng key kahit may button na naka-focus
        protected override bool ProcessCmdKey(ref Message msg, Keys key) // Override lets this form handle keys before default window behavior.
        {
            if (engine.State == GameState.Walking)
            {
                if (key == Keys.W || key == Keys.Up) engine.MovePlayer(0, -1);
                else if (key == Keys.S || key == Keys.Down) engine.MovePlayer(0, 1);
                else if (key == Keys.A || key == Keys.Left) engine.MovePlayer(-1, 0);
                else if (key == Keys.D || key == Keys.Right) engine.MovePlayer(1, 0);
                else return base.ProcessCmdKey(ref msg, key);
                Invalidate();
                return true;
            }
            if (engine.State == GameState.Encounter && ui.HandleKey(key)) return true;
            return base.ProcessCmdKey(ref msg, key);
        }

        protected override void OnPaint(PaintEventArgs e) // Windows Forms calls this when the window needs to be drawn again.
        {
            base.OnPaint(e);
            if (engine.Player == null) return;

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            bool blink = frame % 4 < 2;
            bool elNino = engine.Disaster == Difficulty.ElNino;

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    Rectangle r = TileRect(x, y);
                    Tile t = engine.Map.GetTile(x, y);

                    // 1. lupa
                    if (t == Tile.Wall) Draw(g, "wall", r);
                    else if (t == Tile.High) Draw(g, "high", r);
                    else if (t == Tile.Evac) DrawEvac(g, r, blink);
                    else Draw(g, elNino ? "grass" : "floor", r);

                    // 2. sakuna: babala muna, tapos delikado na
                    if (engine.Map.IsDanger(x, y))
                        Draw(g, engine.Map.Current.DangerSprite, r);
                    else if (engine.Map.IsWarning(x, y))
                    {
                        if (elNino)   // El Niño: unti-unting nagiging kayumanggi ang damo
                            using (var dry = new SolidBrush(Color.FromArgb(150, 160, 110, 40))) g.FillRectangle(dry, r); // using releases drawing resources after use.
                        else if (blink)   // ibang sakuna: kumikislap na pula
                            using (var red = new SolidBrush(Color.FromArgb(120, 255, 60, 60))) g.FillRectangle(red, r);
                    }
                }
            }

            // 3. pamilyang hindi pa nasasagip
            foreach (FamilyMember f in engine.Family)
                if (!f.Rescued) Draw(g, f.Sprite, TileRect(f.X, f.Y));

            // 4. player (kumikislap kapag kakatama lang)
            if (engine.Player.Invincible == 0 || blink)
                Draw(g, engine.Player.Sprite, TileRect(engine.Player.X, engine.Player.Y));

            // 5. makapal na border at hint sa ilalim
            using (var border = new Pen(UIManager.Ink, 4))
                g.DrawRectangle(border, LEFT - 2, TOP - 2, TILE * 12 + 3, TILE * 12 + 3);
            string hint = engine.ExitOpen ? "Bukas na ang EVAC! Takbo!" : "Sagipin sina Lola, Bunso at Bantay. Iwasan ang kumikislap!";
            TextRenderer.DrawText(g, hint, hintFont, new Rectangle(0, TOP + TILE * 12 + 8, ClientSize.Width, 24),
                UIManager.Yellow, TextFormatFlags.HorizontalCenter);
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
