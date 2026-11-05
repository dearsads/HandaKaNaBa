using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HandaKaNaBa
{
    public class PixelLabel : Label
    {
        public Color FaceColor = Color.Transparent;
        public int BorderSize = 0;
        public bool Shadow = false;
        public bool TextShadow = false;

        public PixelLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        private static StringFormat FormatFor(ContentAlignment a)
        {
            var sf = new StringFormat(StringFormatFlags.NoClip);

            switch (a)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    sf.LineAlignment = StringAlignment.Near; break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    sf.LineAlignment = StringAlignment.Far; break;
                default:
                    sf.LineAlignment = StringAlignment.Center; break;
            }

            switch (a)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    sf.Alignment = StringAlignment.Near; break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    sf.Alignment = StringAlignment.Far; break;
                default:
                    sf.Alignment = StringAlignment.Center; break;
            }

            return sf;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int s = Shadow ? 5 : 0;
            Rectangle face = new Rectangle(0, 0, Width - s, Height - s);

            if (Shadow)
            {
                using (var b = new SolidBrush(UIManager.Ink))
                    g.FillRectangle(b, s, s, face.Width, face.Height);
            }

            if (FaceColor != Color.Transparent)
            {
                using (var b = new SolidBrush(FaceColor))
                    g.FillRectangle(b, face);
            }

            if (BorderSize > 0)
            {
                using (var b = new SolidBrush(UIManager.Ink))
                {
                    g.FillRectangle(b, face.X, face.Y, face.Width, BorderSize);
                    g.FillRectangle(b, face.X, face.Bottom - BorderSize, face.Width, BorderSize);
                    g.FillRectangle(b, face.X, face.Y, BorderSize, face.Height);
                    g.FillRectangle(b, face.Right - BorderSize, face.Y, BorderSize, face.Height);
                }
            }

            var textRect = new RectangleF(
                face.X + BorderSize + Padding.Left,
                face.Y + BorderSize + Padding.Top,
                face.Width - BorderSize * 2 - Padding.Horizontal,
                face.Height - BorderSize * 2 - Padding.Vertical);

            using (var sf = FormatFor(TextAlign))
            {
                if (TextShadow)
                {
                    var r2 = textRect;
                    r2.Offset(4, 4);
                    using (var b = new SolidBrush(UIManager.Ink))
                        g.DrawString(Text, Font, b, r2, sf);
                }

                using (var b = new SolidBrush(ForeColor))
                    g.DrawString(Text, Font, b, textRect, sf);
            }
        }
    }

    public class PixelButton : Button
    {
        public Color FaceColor = Color.White;
        public int BorderThickness = 4;

        private bool hover, pressed;
        private const int Depth = 6;

        public PixelButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        protected override bool ShowFocusCues { get { return false; } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int o = pressed ? Depth - 2 : 0;
            Rectangle face = new Rectangle(o, o, Width - Depth, Height - Depth);


            using (var b = new SolidBrush(UIManager.Ink))
                g.FillRectangle(b, Depth, Depth, Width - Depth, Height - Depth);

            // face (brighter on hover)
            Color fill = hover && !pressed
                ? ControlPaint.Light(FaceColor, 0.35f)
                : FaceColor;

            using (var b = new SolidBrush(fill))
                g.FillRectangle(b, face);

            // chunky border
            int t = BorderThickness;
            using (var b = new SolidBrush(UIManager.Ink))
            {
                g.FillRectangle(b, face.X, face.Y, face.Width, t);
                g.FillRectangle(b, face.X, face.Bottom - t, face.Width, t);
                g.FillRectangle(b, face.X, face.Y, t, face.Height);
                g.FillRectangle(b, face.Right - t, face.Y, t, face.Height);
            }

            var textRect = new RectangleF(
                face.X + t + 2, face.Y + t + 2,
                face.Width - (t + 2) * 2, face.Height - (t + 2) * 2);

            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            using (var b = new SolidBrush(ForeColor))
            {
                g.DrawString(Text, Font, b, textRect, sf);
            }
        }
    }

    // =====================================================================
    //  UI MANAGER
    // =====================================================================

    public class UIManager
    {
        // ---- Palette ----
        public static readonly Color Ink = Color.FromArgb(17, 17, 17);
        public static readonly Color Blue = ColorTranslator.FromHtml("#2391FF");
        public static readonly Color Yellow = ColorTranslator.FromHtml("#FFF83F");
        public static readonly Color Gold = ColorTranslator.FromHtml("#FFCD54");
        public static readonly Color Red = ColorTranslator.FromHtml("#F70063");

        public const int PanicSeconds = 10;

        private Form form;
        private GameEngine engine;

        private Panel menuPanel, hudPanel, encounterPanel, endPanel;
        private PixelLabel lblHearts, lblName, lblSagip, lblScore, lblCombo, lblTime;
        private PixelLabel lblTimer, lblWho;
        private PixelLabel lblScenario, lblResult, lblEndTitle, lblStars;

        private PixelButton[] cardButtons = new PixelButton[3];
        private PixelButton btnContinue;
        private TextBox txtReport;

        private System.Windows.Forms.Timer panicTimer =
            new System.Windows.Forms.Timer { Interval = 1000 };

        private int timeLeft;
        private bool picked;

        public UIManager(Form form, GameEngine engine)
        {
            this.form = form;
            this.engine = engine;

            BuildMenu();
            BuildHud();
            BuildEncounter();
            BuildEnd();

            engine.Changed += UpdateHud;
            engine.EncounterStarted += ShowEncounter;
            engine.Ended += ShowEnd;

            panicTimer.Tick += PanicTick;

            ShowOnly(menuPanel);
        }

        // ---------------- FONTS ----------------

        private static bool? pixelInstalled;

        private static bool IsPixelInstalled()
        {
            if (pixelInstalled == null)
            {
                using (var fonts = new InstalledFontCollection())
                {
                    pixelInstalled = fonts.Families.Any(f =>
                        string.Equals(f.Name, "Pixelify Sans",
                            StringComparison.OrdinalIgnoreCase));
                }
            }
            return pixelInstalled.Value;
        }

        private static Font Pixel(float size, FontStyle style = FontStyle.Bold)
        {
            return new Font(
                IsPixelInstalled() ? "Pixelify Sans" : "Consolas",
                size,
                style);
        }

        // ---------------- HELPERS ----------------

        private PixelLabel MakeLabel(
            Control parent,
            string text,
            int x, int y, int w, int h,
            Font font,
            Color face,
            int border = 0,
            bool shadow = false)
        {
            var lbl = new PixelLabel
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = font,
                FaceColor = face,
                BorderSize = border,
                Shadow = shadow,
                ForeColor = Ink,
                TextAlign = ContentAlignment.MiddleCenter
            };

            parent.Controls.Add(lbl);
            return lbl;
        }

        private PixelButton MakeButton(
            Control parent,
            string text,
            int x, int y, int w, int h,
            Color face,
            Font font)
        {
            var btn = new PixelButton
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = font,
                FaceColor = face,
                ForeColor = Ink
            };

            parent.Controls.Add(btn);
            return btn;
        }

        private Panel MakePanel(
            Color back,
            Point location,
            Size size,
            DockStyle dock)
        {
            var p = new Panel
            {
                BackColor = back,
                Location = location,
                Size = size,
                Dock = dock,
                Visible = false
            };

            form.Controls.Add(p);
            return p;
        }

        // thick pixel frame around a panel
        private static void AddFrame(Panel p, int thickness)
        {
            p.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Ink))
                {
                    var r = p.ClientRectangle;
                    e.Graphics.FillRectangle(b, 0, 0, r.Width, thickness);
                    e.Graphics.FillRectangle(b, 0, r.Height - thickness, r.Width, thickness);
                    e.Graphics.FillRectangle(b, 0, 0, thickness, r.Height);
                    e.Graphics.FillRectangle(b, r.Width - thickness, 0, thickness, r.Height);
                }
            };
        }

        private void ShowOnly(Panel p)
        {
            menuPanel.Visible = p == menuPanel;
            hudPanel.Visible = p == hudPanel;
            endPanel.Visible = p == endPanel;
            encounterPanel.Visible = false;

            p.BringToFront();
        }

        // ---------------- MENU ----------------

        private void BuildMenu()
        {
            menuPanel = MakePanel(
                Blue,
                Point.Empty,
                Size.Empty,
                DockStyle.Fill);

            string backgroundPath = Path.Combine(
                Application.StartupPath,
                "Assets",
                "menu-background.png");

            if (File.Exists(backgroundPath))
            {
                using (Image image = Image.FromFile(backgroundPath))
                {
                    menuPanel.BackgroundImage = new Bitmap(image);
                }

                menuPanel.BackgroundImageLayout = ImageLayout.Stretch;
            }

            string titlePath = Path.Combine(
                Application.StartupPath,
                "Assets",
                "menu title.png");

            if (File.Exists(titlePath))
            {
                var titleImage = new PictureBox
                {
                    Location = new Point(24, 8),
                    Size = new Size(600, 240),    
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };

                using (Image image = Image.FromFile(titlePath))
                {
                    titleImage.Image = new Bitmap(image);
                }

                menuPanel.Controls.Add(titleImage);
            }

            var heading = MakeLabel(
                menuPanel,
                "PUMILI NG SAKUNA",
                84, 254, 440, 52,
                Pixel(22),
                Red,
                4,
                true);

            heading.ForeColor = Color.White;

            string[] names =
            {
                "TSUNAMI\n\nEASY",
                "LINDOL\n\nMEDIUM",
                "EL NIÑO\n\nHARD"
            };

            Color[] colors = { Yellow, Gold, Red };

            for (int i = 0; i < 3; i++)
            {
                Difficulty d = (Difficulty)i;

                var btn = MakeButton(
                    menuPanel,
                    names[i],
                    16 + i * 200,
                    320,
                    182,
                    170,
                    colors[i],
                    Pixel(21));

                if (i == 2)
                    btn.ForeColor = Color.White;

                btn.Click += (s, e) => StartGame(d);
            }

            MakeLabel(
                menuPanel,
                "WASD / Arrows: gumalaw\n" +
                "1, 2, 3: pumili ng card\n" +
                "Sagipin sina Lola, Bunso at Bantay, tapos tumakbo sa EVACUATION!",
                16, 510, 576, 96,
                Pixel(13),
                Yellow,
                4,
                true);
        }

        private void StartGame(Difficulty d)
        {
            engine.StartLevel(d);

            ShowOnly(hudPanel);
            UpdateHud();

            form.ActiveControl = null;
            form.Focus();
        }

        // ---------------- HUD ----------------

        private void BuildHud()
        {
            hudPanel = new Panel
            {
                BackColor = Gold,
                Dock = DockStyle.Top,
                Height = 64,
                Visible = false
            };

            // chunky bottom line
            hudPanel.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Ink))
                    e.Graphics.FillRectangle(b, 0, hudPanel.Height - 4, hudPanel.Width, 4);
            };

            form.Controls.Add(hudPanel);

            lblHearts = MakeLabel(hudPanel, "", 8, 4, 118, 52,
                new Font("Segoe UI Symbol", 18, FontStyle.Bold), Ink, 3);
            lblHearts.ForeColor = Red;

            lblName = MakeHudCell(134, 120);
            lblSagip = MakeHudCell(260, 80);
            lblScore = MakeHudCell(346, 100);
            lblCombo = MakeHudCell(452, 62);
            lblTime = MakeHudCell(520, 72);
        }

        private PixelLabel MakeHudCell(int x, int w)
        {
            return MakeLabel(hudPanel, "", x, 4, w, 52, Pixel(12), Yellow, 3);
        }

        private void UpdateHud()
        {
            int lives = Math.Max(0, engine.Lives);

            lblHearts.Text =
                new string('\u2665', lives) +
                new string('\u2661', GameEngine.MaxLives - lives);

            lblName.Text = "SAKUNA\n" + engine.Map.Current.Name;
            lblSagip.Text = "SAGIP\n" + engine.RescuedCount + "/3";
            lblScore.Text = "SCORE\n" + engine.Score;
            lblCombo.Text = "COMBO\nx" + engine.Multiplier;
            lblCombo.Font = new Font("Pixelify Sans", 10, FontStyle.Bold);
            lblTime.Text = "ORAS\n" + engine.Seconds + "s";
        }

        public void Tick()
        {
            if (hudPanel.Visible)
                UpdateHud();
        }

        // ---------------- ENCOUNTER + PANIC TIMER ----------------

        private void BuildEncounter()
        {
            encounterPanel = MakePanel(
                Gold,
                new Point(24, 80),
                new Size(560, 560),
                DockStyle.None);

            AddFrame(encounterPanel, 6);

            lblTimer = MakeLabel(
                encounterPanel,
                "10",
                20, 16, 104, 80,
                Pixel(50),
                Yellow,
                4,
                true);

            lblWho = MakeLabel(
                encounterPanel,
                "",
                136, 16, 404, 80,
                Pixel(24),
                Blue,
                4,
                true);

            lblWho.ForeColor = Color.White;
            lblWho.TextShadow = true;

            lblScenario = MakeLabel(
                encounterPanel,
                "",
                20, 104, 520, 116,
                Pixel(15),
                Ink,
                4);

            lblScenario.ForeColor = Color.White;
            lblScenario.Padding = new Padding(8);

            for (int i = 0; i < 3; i++)
            {
                int index = i;

                cardButtons[i] = MakeButton(
                    encounterPanel,
                    "",
                    20 + i * 180,
                    230,
                    168,
                    186,
                    Color.White,
                    Pixel(12));

                cardButtons[i].Click += (s, e) => Choose(index);
            }

            lblResult = MakeLabel(
                encounterPanel,
                "",
                20, 426, 396, 120,
                Pixel(12),
                Color.White,
                4);

            lblResult.TextAlign = ContentAlignment.TopLeft;
            lblResult.Padding = new Padding(6);

            btnContinue = MakeButton(
                encounterPanel,
                "TULOY ▶",
                426, 436, 114, 100,
                Ink,
                Pixel(17));

            btnContinue.ForeColor = Yellow;
            btnContinue.Click += (s, e) => Continue();
        }

        private void ShowEncounter()
        {
            picked = false;
            timeLeft = PanicSeconds;

            lblTimer.Text = timeLeft.ToString();
            lblTimer.ForeColor = Ink;
            lblTimer.FaceColor = Yellow;
            lblTimer.Invalidate();

            lblWho.Text =
                "SAGIPIN SI " + engine.Target.Name.ToUpper() + "!";

            lblScenario.Text = engine.Scenario.Text;

            for (int i = 0; i < 3; i++)
            {
                Card c = engine.Scenario.Cards[i];

                cardButtons[i].Text =
                    "[" + (i + 1) + "]\n" +
                    c.Title + "\n\n" + c.Hint;

                cardButtons[i].FaceColor = Color.White;
                cardButtons[i].BorderThickness = 4;
                cardButtons[i].Invalidate();
            }

            lblResult.Visible = false;
            btnContinue.Visible = false;

            encounterPanel.Visible = true;
            encounterPanel.BringToFront();

            form.ActiveControl = null;
            panicTimer.Start();
        }

        private void PanicTick(object sender, EventArgs e)
        {
            timeLeft--;

            lblTimer.Text = Math.Max(0, timeLeft).ToString();

            if (timeLeft <= 3)
            {
                lblTimer.FaceColor = Red;         
                lblTimer.ForeColor = Color.White;
                lblTimer.Invalidate();
            }

            if (timeLeft <= 0)
                Choose(-1);
        }

        private void Choose(int index)
        {
            if (picked)
                return;

            picked = true;
            panicTimer.Stop();

            engine.ChooseCard(index, Math.Max(0, timeLeft));

            for (int i = 0; i < 3; i++)
            {
                Card c = engine.Scenario.Cards[i];

                cardButtons[i].Text = c.Label + "\n\n" + c.Title;
                cardButtons[i].FaceColor = c.Color;
                cardButtons[i].BorderThickness = i == index ? 8 : 3;
                cardButtons[i].Invalidate();
            }

            string reaction = index >= 0
                ? engine.Scenario.Cards[index].Reaction
                : "Na-freeze ka! Naubos ang oras.";

            lblResult.Text =
                reaction + "\n\nAral: " +
                engine.Scenario.BestCard.Lesson + "\n" +
                engine.ResultText;

            lblResult.Visible = true;
            btnContinue.Visible = true;
        }

        private void Continue()
        {
            if (!picked)
                return;

            encounterPanel.Visible = false;
            engine.Resume();

            form.ActiveControl = null;
            form.Focus();
        }

        public bool HandleKey(Keys key)
        {
            if (!encounterPanel.Visible)
                return false;

            if (!picked && key >= Keys.D1 && key <= Keys.D3)
            {
                Choose((int)key - (int)Keys.D1);
                return true;
            }

            if (picked && (key == Keys.Enter || key == Keys.Space))
            {
                Continue();
                return true;
            }

            return false;
        }

        // ---------------- END SCREEN + REPORT ----------------

        private void BuildEnd()
        {
            endPanel = MakePanel(
                Gold,
                Point.Empty,
                Size.Empty,
                DockStyle.Fill);

            lblEndTitle = MakeLabel(
                endPanel,
                "",
                16, 12, 576, 84,
                Pixel(36),
                Color.Transparent);

            lblEndTitle.TextShadow = true;
            lblEndTitle.ForeColor = Color.White;

            lblStars = MakeLabel(
                endPanel,
                "",
                16, 98, 576, 56,
                new Font("Segoe UI Symbol", 32, FontStyle.Bold),
                Color.Transparent);

            txtReport = new TextBox
            {
                Location = new Point(40, 160),
                Size = new Size(528, 400),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = Pixel(13, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Ink,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };

            endPanel.Controls.Add(txtReport);

            MakeButton(
                endPanel,
                "ULITIN",
                40, 576, 250, 64,
                Yellow,
                Pixel(26)).Click += (s, e) => StartGame(engine.Disaster);

            MakeButton(
                endPanel,
                "MENU",
                318, 576, 250, 64,
                Color.White,
                Pixel(26)).Click += (s, e) => ShowOnly(menuPanel);
        }

        private void ShowEnd()
        {
            panicTimer.Stop();

            bool won = engine.State == GameState.Clear;

            endPanel.BackColor = won ? Gold : Red;

            lblEndTitle.Text = won
                ? "NAKALIGTAS!"
                : "SABLAY! UBOS ANG PUSO";

            lblStars.ForeColor = won ? Ink : Color.White;

            lblStars.Text = won
                ? new string('★', engine.Stars) +
                  new string('☆', 3 - engine.Stars)
                : "";

            var lines = new List<string>(engine.Summary)
            {
                "",
                "=== BARANGAY ASSEMBLY REPORT ==="
            };

            lines.Add(
                engine.Database.GetReport(
                    engine.Disaster,
                    engine.Stars));

            txtReport.Text = string.Join(
                Environment.NewLine,
                lines);

            ShowOnly(endPanel);
        }
    }
}