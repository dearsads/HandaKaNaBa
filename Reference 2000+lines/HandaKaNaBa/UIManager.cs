// =====================================================================
//  UIManager.cs
//  OWNER: Member 3 - Front-End (UI & Timers)
//
//  All screens are Panels on the ONE window (no extra forms):
//    menuPanel       - title + difficulty select
//    hudPanel        - hearts, score, combo, rescued count (top bar)
//    encounterPanel  - the card phase with the 10-second PANIC TIMER
//    endPanel        - level clear / game over + Barangay Assembly Report
//  The grid itself is drawn by MainForm. This class also draws the
//  hazard banner and the floating popups on top of the grid.
//
//  The UI only READS the engine and calls its methods. It never
//  changes hearts or score directly.
// =====================================================================
using System.Drawing.Drawing2D;

namespace HandaKaNaBa
{
    /// <summary>A Panel that does not flicker when it repaints.</summary>
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    /// <summary>A PictureBox that keeps pixel art sharp. Animated GIFs play automatically.</summary>
    public class PixelBox : PictureBox
    {
        public PixelBox()
        {
            SizeMode = PictureBoxSizeMode.Zoom;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(pe);
        }
    }

    public class UIManager
    {
        // ---- Pop palette (used by every file) ----
        public static readonly Color Ink = Color.FromArgb(17, 17, 17);
        public static readonly Color Cream = Color.FromArgb(255, 247, 214);
        public static readonly Color Yellow = Color.FromArgb(253, 224, 71);
        public static readonly Color Cyan = Color.FromArgb(34, 211, 238);
        public static readonly Color Pink = Color.FromArgb(244, 114, 182);
        public static readonly Color Green = Color.FromArgb(52, 211, 153);
        public static readonly Color Orange = Color.FromArgb(251, 146, 60);
        public static readonly Color Red = Color.FromArgb(220, 38, 38);

        /// <summary>Seconds to pick a card. Raise to 12 if players can't finish reading.</summary>
        public const int PanicSeconds = 10;

        private readonly Form form;
        private readonly GameEngine engine;

        private readonly BufferedPanel menuPanel = new BufferedPanel();
        private readonly BufferedPanel hudPanel = new BufferedPanel();
        private readonly BufferedPanel encounterPanel = new BufferedPanel();
        private readonly BufferedPanel endPanel = new BufferedPanel();

        private readonly System.Windows.Forms.Timer panicTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        private int timeLeft;
        private bool picked;
        private int pickedIndex = -1;
        private Scenario shownScenario;
        private int frame;

        // encounter controls
        private Label lblTimer, lblWho, lblScenario, lblReaction;
        private PixelBox memeBox;
        private readonly Button[] cardButtons = new Button[3];
        private Button btnContinue;

        // end controls
        private Label lblEndTitle, lblStars, lblRank;
        private TextBox txtReport;
        private Button btnRetry, btnMenu;

        // popups
        private class Toast { public string Text; public Color Color; public int Ticks; }
        private readonly List<Toast> toasts = new List<Toast>();

        public UIManager(Form form, GameEngine engine)
        {
            this.form = form;
            this.engine = engine;

            BuildMenu();
            BuildHud();
            BuildEncounter();
            BuildEnd();

            engine.LivesChanged += () => hudPanel.Invalidate();
            engine.EncounterStarted += ShowEncounter;
            engine.LevelEnded += ShowEnd;
            engine.Popup += (text, color) => toasts.Add(new Toast { Text = text, Color = color, Ticks = 22 });
            panicTimer.Tick += PanicTimer_Tick;

            ShowMenu();
        }

        // ===============================================================
        //  SCREEN FLOW
        // ===============================================================
        public void ShowMenu()
        {
            engine.ReturnToMenu();
            hudPanel.Visible = false;
            encounterPanel.Visible = false;
            endPanel.Visible = false;
            menuPanel.Visible = true;
            menuPanel.BringToFront();
            menuPanel.Invalidate(true);
        }

        private void StartGame(Difficulty disaster)
        {
            toasts.Clear();
            engine.StartLevel(disaster);
            menuPanel.Visible = false;
            endPanel.Visible = false;
            encounterPanel.Visible = false;
            hudPanel.Visible = true;
            hudPanel.BringToFront();
            form.ActiveControl = null;     // so the keyboard goes to the game, not a button
            form.Focus();
            form.Invalidate();
        }

        /// <summary>Called by MainForm every game tick (100 ms).</summary>
        public void Tick()
        {
            frame++;
            foreach (Toast t in toasts) t.Ticks--;
            toasts.RemoveAll(t => t.Ticks <= 0);

            if (hudPanel.Visible) hudPanel.Invalidate();

            if (encounterPanel.Visible && !picked && timeLeft <= 3)
                lblTimer.ForeColor = frame % 4 < 2 ? Red : Ink;
        }

        // ===============================================================
        //  MENU
        // ===============================================================
        private void BuildMenu()
        {
            menuPanel.Dock = DockStyle.Fill;
            menuPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                PaintHalftone(g, menuPanel.ClientRectangle, Cyan);

                // Title in a tilted white box
                g.SmoothingMode = SmoothingMode.AntiAlias;
                GraphicsState saved = g.Save();
                g.TranslateTransform(menuPanel.Width / 2f, 96);
                g.RotateTransform(-2);
                var box = new Rectangle(-260, -50, 520, 100);
                g.FillRectangle(new SolidBrush(Ink), box.X + 8, box.Y + 8, box.Width, box.Height);
                g.FillRectangle(Brushes.White, box);
                g.DrawRectangle(new Pen(Ink, 5), box);
                // TextRenderer ignores rotation, so the tilted title uses DrawString
                using (Font title = Sprites.Display(52))
                using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var shadow = new RectangleF(box.X + 4, box.Y + 4, box.Width, box.Height);
                    g.DrawString("HANDA KA NA BA?", title, new SolidBrush(Ink), shadow, center);
                    g.DrawString("HANDA KA NA BA?", title, new SolidBrush(Pink), box, center);
                }
                g.Restore(saved);

                var strip = new Rectangle(104, 168, 400, 36);
                g.FillRectangle(new SolidBrush(Ink), strip);
                using (var f = new Font("Segoe UI", 12, FontStyle.Bold))
                    DrawCentered(g, "PUMILI NG SAKUNA", f, Color.White, strip, 0);
            };

            var levels = new[] { Difficulty.Tsunami, Difficulty.Earthquake, Difficulty.Volcano };
            for (int i = 0; i < 3; i++)
            {
                Difficulty d = levels[i];
                var btn = new Button
                {
                    Location = new Point(16 + i * 200, 232),
                    Size = new Size(176, 220),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = i == 0 ? Color.FromArgb(96, 165, 250) : i == 1 ? Orange : Color.FromArgb(239, 68, 68),
                    Text = "",
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                btn.FlatAppearance.BorderSize = 4;
                btn.FlatAppearance.BorderColor = Ink;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btn.BackColor);
                btn.Paint += (s, e) => PaintDifficultyButton(e.Graphics, btn.ClientRectangle, d);
                btn.Click += (s, e) => StartGame(d);
                menuPanel.Controls.Add(btn);
            }

            var help = new Label
            {
                Location = new Point(16, 480),
                Size = new Size(576, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Ink,
                Padding = new Padding(10),
                Text = "WASD / Arrow keys: gumalaw\r\n" +
                       "Sagipin sina Lola, Bunso at Bantay. Bawat sagip = 3 kard, " + PanicSeconds + " segundo. Pindutin ang 1, 2 o 3.\r\n" +
                       "Iwasan ang kumikislap na tiles. Kunin ang Go-Bag para sa shield.\r\n" +
                       "Kapag nasagip na ang lahat, tumakbo sa EVAC!"
            };
            menuPanel.Controls.Add(help);

            form.Controls.Add(menuPanel);
        }

        private void PaintDifficultyButton(Graphics g, Rectangle r, Difficulty d)
        {
            string name = d == Difficulty.Tsunami ? "TSUNAMI" : d == Difficulty.Earthquake ? "LINDOL" : "BULKAN";
            string level = d == Difficulty.Tsunami ? "EASY" : d == Difficulty.Earthquake ? "MEDIUM" : "HARD";
            string desc = d == Difficulty.Tsunami ? "Alon mula sa dagat. Umakyat sa burol!"
                        : d == Difficulty.Earthquake ? "Bitak na lumalala habang tumatagal."
                        : "Pagsabog na 3x3. Bantayan ang anino!";
            string sprite = d == Difficulty.Tsunami ? "water" : d == Difficulty.Earthquake ? "crack" : "lava";

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            Image icon = Sprites.Get(sprite);
            if (icon != null) g.DrawImage(icon, new Rectangle(r.Width / 2 - 28, 14, 56, 56));

            using (Font big = Sprites.Display(30))
                DrawCentered(g, name, big, Ink, new Rectangle(0, 74, r.Width, 40), 0);

            var badge = new Rectangle(r.Width / 2 - 40, 118, 80, 24);
            g.FillRectangle(Brushes.White, badge);
            g.DrawRectangle(new Pen(Ink, 2), badge);
            using (var f = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                DrawCentered(g, level, f, Ink, badge, 0);
                TextRenderer.DrawText(g, desc, f, new Rectangle(10, 148, r.Width - 20, 40), Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                TextRenderer.DrawText(g, "BEST: " + GameEngine.BestScore(d), f, new Rectangle(0, 192, r.Width, 20), Ink,
                    TextFormatFlags.HorizontalCenter);
            }
        }

        // ===============================================================
        //  HUD (top bar)
        // ===============================================================
        private void BuildHud()
        {
            hudPanel.Dock = DockStyle.Top;
            hudPanel.Height = 64;
            hudPanel.BackColor = Cream;
            hudPanel.Paint += (s, e) => PaintHud(e.Graphics);
            form.Controls.Add(hudPanel);
        }

        private void PaintHud(Graphics g)
        {
            if (engine.Player == null) return;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillRectangle(new SolidBrush(Ink), 0, hudPanel.Height - 4, hudPanel.Width, 4);

            // Hearts
            Image full = Sprites.Get("heart_full");
            Image empty = Sprites.Get("heart_empty");
            for (int i = 0; i < GameEngine.MaxLives; i++)
            {
                var r = new Rectangle(10 + i * 28, 18, 26, 26);
                Image img = i < engine.Lives ? full : empty;
                if (img != null) g.DrawImage(img, r);
                else using (var f = new Font("Segoe UI Symbol", 16, FontStyle.Bold))
                    TextRenderer.DrawText(g, "♥", f, r, i < engine.Lives ? Pink : Color.LightGray);
            }

            // Disaster tag with danger level
            var tag = new Rectangle(160, 14, 150, 34);
            g.FillRectangle(new SolidBrush(Yellow), tag);
            g.DrawRectangle(new Pen(Ink, 3), tag);
            int intensity = engine.Map.Current != null ? engine.Map.Current.Intensity : 1;
            using (Font f = Sprites.Display(20))
                DrawCentered(g, (engine.Map.Current?.Name ?? "") + "  LVL " + intensity, f, Ink, tag, 0);

            // Rescued
            var sagip = new Rectangle(318, 14, 90, 34);
            g.FillRectangle(new SolidBrush(engine.ExitOpen ? Green : Color.White), sagip);
            g.DrawRectangle(new Pen(Ink, 3), sagip);
            using (Font f = Sprites.Display(18))
                DrawCentered(g, "SAGIP " + engine.RescuedCount + "/" + engine.Family.Count, f, Ink, sagip, 0);

            // Score, combo, time, shield
            using (Font f = Sprites.Display(24))
                TextRenderer.DrawText(g, engine.Score + " PTS", f, new Rectangle(414, 4, 186, 30), Ink, TextFormatFlags.Right);
            string line2 = "x" + engine.Multiplier + " COMBO  ·  " + engine.Seconds + "s" + (engine.Player.HasShield ? "  ·  SHIELD" : "");
            using (var f = new Font("Segoe UI", 9, FontStyle.Bold))
                TextRenderer.DrawText(g, line2, f, new Rectangle(414, 36, 186, 20), Ink, TextFormatFlags.Right);
        }

        // ===============================================================
        //  ENCOUNTER (card phase + panic timer)
        // ===============================================================
        private void BuildEncounter()
        {
            encounterPanel.Location = new Point(MainForm.GridLeft + 8, MainForm.GridTop + 8);
            encounterPanel.Size = new Size(560, 560);
            encounterPanel.Visible = false;
            encounterPanel.Paint += (s, e) =>
            {
                PaintHalftone(e.Graphics, encounterPanel.ClientRectangle, Yellow);
                e.Graphics.DrawRectangle(new Pen(Ink, 6), 3, 3, encounterPanel.Width - 7, encounterPanel.Height - 7);
            };

            lblTimer = new Label
            {
                Location = new Point(16, 10), Size = new Size(100, 90),
                Font = Sprites.Display(64), ForeColor = Ink, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblWho = new Label
            {
                Location = new Point(122, 26), Size = new Size(316, 56),
                Font = Sprites.Display(30), ForeColor = Ink, BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleCenter
            };
            memeBox = new PixelBox { Location = new Point(448, 12), Size = new Size(96, 96) };
            lblScenario = new Label
            {
                Location = new Point(16, 112), Size = new Size(528, 104),
                Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.White, BackColor = Ink,
                Padding = new Padding(10), TextAlign = ContentAlignment.MiddleLeft
            };
            encounterPanel.Controls.AddRange(new Control[] { lblTimer, lblWho, memeBox, lblScenario });

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var btn = new Button
                {
                    Location = new Point(16 + i * 182, 228),
                    Size = new Size(164, 196),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    Text = "",
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                btn.FlatAppearance.BorderSize = 4;
                btn.FlatAppearance.BorderColor = Ink;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(252, 200, 225);
                btn.Paint += (s, e) => PaintCard(e.Graphics, btn, index);
                btn.Click += (s, e) => Choose(index);
                cardButtons[i] = btn;
                encounterPanel.Controls.Add(btn);
            }

            lblReaction = new Label
            {
                Location = new Point(16, 434), Size = new Size(400, 112),
                Font = new Font("Segoe UI", 9.75f), ForeColor = Ink, BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Visible = false
            };
            btnContinue = new Button
            {
                Location = new Point(426, 452), Size = new Size(118, 76),
                FlatStyle = FlatStyle.Flat, BackColor = Ink, ForeColor = Yellow,
                Font = Sprites.Display(24), Text = "TULOY ▶", Visible = false, TabStop = false
            };
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.Click += (s, e) => Continue();
            encounterPanel.Controls.AddRange(new Control[] { lblReaction, btnContinue });

            form.Controls.Add(encounterPanel);
        }

        private void ShowEncounter()
        {
            shownScenario = engine.CurrentScenario;
            picked = false;
            pickedIndex = -1;
            timeLeft = PanicSeconds;

            lblTimer.Text = timeLeft.ToString();
            lblTimer.ForeColor = Ink;
            lblWho.Text = "SAGIPIN SI " + engine.CurrentTarget.Name.ToUpper() + "!";
            lblScenario.Text = shownScenario.TextFor(engine.CurrentTarget.Name);
            memeBox.Image = Sprites.Get("face_alert");
            lblReaction.Visible = false;
            btnContinue.Visible = false;

            foreach (Button b in cardButtons)
            {
                b.BackColor = Color.White;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(252, 200, 225);
                b.FlatAppearance.BorderSize = 4;
                b.Invalidate();
            }

            encounterPanel.Visible = true;
            encounterPanel.BringToFront();
            form.ActiveControl = null;
            panicTimer.Start();
            hudPanel.Invalidate();
        }

        private void PanicTimer_Tick(object sender, EventArgs e)
        {
            if (picked) return;
            timeLeft--;
            lblTimer.Text = Math.Max(0, timeLeft).ToString();
            if (timeLeft <= 0) Choose(-1);
        }

        /// <summary>index 0-2 = a card, -1 = the timer ran out.</summary>
        private void Choose(int index)
        {
            if (picked || shownScenario == null) return;
            picked = true;
            pickedIndex = index;
            panicTimer.Stop();

            CardResult result = engine.ChooseCard(index, Math.Max(0, timeLeft));
            Card card = engine.LastCard;

            // Flip all 3 cards so the player sees why each was right or wrong
            for (int i = 0; i < 3; i++)
            {
                Card c = shownScenario.Cards[i];
                cardButtons[i].BackColor = c.Color;
                cardButtons[i].FlatAppearance.MouseOverBackColor = c.Color;
                cardButtons[i].FlatAppearance.BorderSize = i == index ? 7 : 3;
                cardButtons[i].Invalidate();
            }

            memeBox.Image = Sprites.Get(result == CardResult.Handa ? "face_handa"
                                      : result == CardResult.PwedeNa ? "face_pwede" : "face_sablay");

            string reaction = card != null ? card.Reaction : EncounterDatabase.TimeoutReaction;
            lblReaction.Text = reaction + "\r\nAral: " + shownScenario.BestCard.Lesson + "\r\n" + engine.LastScoreText;
            lblReaction.Visible = true;
            btnContinue.Visible = true;
            lblTimer.ForeColor = Ink;
            hudPanel.Invalidate();
        }

        private void Continue()
        {
            if (!picked) return;
            encounterPanel.Visible = false;
            shownScenario = null;
            engine.ResumeAfterEncounter();
            form.ActiveControl = null;
            form.Focus();
            hudPanel.Invalidate();
            form.Invalidate();
        }

        /// <summary>Keyboard during an encounter: 1/2/3 pick a card, Enter/Space continue.</summary>
        public bool HandleEncounterKey(Keys key)
        {
            if (!encounterPanel.Visible) return false;
            if (!picked)
            {
                if (key == Keys.D1 || key == Keys.NumPad1) { Choose(0); return true; }
                if (key == Keys.D2 || key == Keys.NumPad2) { Choose(1); return true; }
                if (key == Keys.D3 || key == Keys.NumPad3) { Choose(2); return true; }
            }
            else if (key == Keys.Enter || key == Keys.Space)
            {
                Continue();
                return true;
            }
            return false;
        }

        private void PaintCard(Graphics g, Button btn, int index)
        {
            if (shownScenario == null) return;
            Card card = shownScenario.Cards[index];
            Rectangle r = btn.ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Number badge
            var badge = new Rectangle(8, 8, 30, 30);
            g.FillEllipse(new SolidBrush(Ink), badge);
            using (Font f = Sprites.Display(18))
                DrawCentered(g, (index + 1).ToString(), f, Yellow, badge, 0);

            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak;
            if (!picked)
            {
                using (var title = new Font("Segoe UI", 11, FontStyle.Bold))
                    TextRenderer.DrawText(g, card.Title, title, new Rectangle(10, 48, r.Width - 20, 90), Ink, flags);
                using (var hint = new Font("Segoe UI", 9, FontStyle.Italic))
                    TextRenderer.DrawText(g, card.Hint, hint, new Rectangle(10, 140, r.Width - 20, 48), Color.FromArgb(70, 70, 70), flags);
            }
            else
            {
                using (Font label = Sprites.Display(30))
                    TextRenderer.DrawText(g, card.Label, label, new Rectangle(0, 44, r.Width, 44), Ink, TextFormatFlags.HorizontalCenter);
                using (var title = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    TextRenderer.DrawText(g, card.Title, title, new Rectangle(10, 96, r.Width - 20, 90), Ink, flags);
                if (index == pickedIndex)
                {
                    var you = new Rectangle(r.Width - 62, 8, 54, 24);
                    g.FillRectangle(new SolidBrush(Ink), you);
                    using (Font f = Sprites.Display(16))
                        DrawCentered(g, "IKAW", f, Yellow, you, 0);
                }
            }
        }

        // ===============================================================
        //  END SCREEN + BARANGAY ASSEMBLY REPORT
        // ===============================================================
        private void BuildEnd()
        {
            endPanel.Dock = DockStyle.Fill;
            endPanel.Visible = false;
            endPanel.Paint += (s, e) =>
                PaintHalftone(e.Graphics, endPanel.ClientRectangle, engine.State == GameState.LevelClear ? Yellow : Pink);

            lblEndTitle = new Label
            {
                Location = new Point(16, 20), Size = new Size(576, 76),
                Font = Sprites.Display(56), ForeColor = Ink, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblStars = new Label
            {
                Location = new Point(16, 96), Size = new Size(576, 56),
                Font = new Font("Segoe UI Symbol", 34, FontStyle.Bold), ForeColor = Ink, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblRank = new Label
            {
                Location = new Point(40, 154), Size = new Size(528, 40),
                Font = Sprites.Display(24), ForeColor = Color.White, BackColor = Ink,
                TextAlign = ContentAlignment.MiddleCenter
            };
            txtReport = new TextBox
            {
                Location = new Point(40, 204), Size = new Size(528, 360),
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10.5f), BackColor = Color.White, ForeColor = Ink,
                BorderStyle = BorderStyle.FixedSingle, TabStop = false
            };
            btnRetry = MakeBigButton("ULITIN", new Point(40, 580), Cyan);
            btnRetry.Click += (s, e) => StartGame(engine.Disaster);
            btnMenu = MakeBigButton("MENU", new Point(318, 580), Color.White);
            btnMenu.Click += (s, e) => ShowMenu();

            endPanel.Controls.AddRange(new Control[] { lblEndTitle, lblStars, lblRank, txtReport, btnRetry, btnMenu });
            form.Controls.Add(endPanel);
        }

        private Button MakeBigButton(string text, Point location, Color color)
        {
            var b = new Button
            {
                Location = location, Size = new Size(250, 64),
                FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Ink,
                Font = Sprites.Display(28), Text = text, Cursor = Cursors.Hand, TabStop = false
            };
            b.FlatAppearance.BorderSize = 4;
            b.FlatAppearance.BorderColor = Ink;
            return b;
        }

        private void ShowEnd()
        {
            panicTimer.Stop();
            encounterPanel.Visible = false;
            bool cleared = engine.State == GameState.LevelClear;

            lblEndTitle.Text = cleared ? "NAKALIGTAS!" : "SABLAY! UBOS ANG PUSO";
            lblStars.Text = cleared ? new string('★', engine.Stars) + new string('☆', 3 - engine.Stars) : "";
            lblRank.Text = cleared
                ? EncounterDatabase.GetRank(engine.Stars) + "  ·  " + engine.Score + " PTS" + (engine.NewBest ? "  ·  NEW BEST!" : "")
                : engine.Score + " PTS  ·  Subukan ulit!";

            var lines = new List<string>();
            lines.AddRange(engine.Breakdown);
            lines.Add("");
            lines.Add("=== BARANGAY ASSEMBLY REPORT ===");
            lines.Add(engine.Database.GetBarangayAssemblyReport(engine.Disaster, engine.Stars));
            txtReport.Text = string.Join(Environment.NewLine, lines).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

            hudPanel.Visible = false;
            endPanel.Visible = true;
            endPanel.BringToFront();
            endPanel.Invalidate();
        }

        // ===============================================================
        //  DRAWN ON TOP OF THE GRID (called from MainForm.OnPaint)
        // ===============================================================
        public void DrawOverlay(Graphics g, Rectangle grid)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Hazard banner, blinking
            string banner = engine.Map.Current?.Banner;
            if (banner != null && engine.State == GameState.Walking)
            {
                var strip = new Rectangle(grid.Left, grid.Top + 10, grid.Width, 44);
                g.FillRectangle(new SolidBrush(Ink), strip);
                using (Font f = Sprites.Display(26))
                    DrawCentered(g, banner, f, frame % 6 < 3 ? Yellow : Pink, strip, 0);
            }

            // Popups, newest at the bottom
            int y = grid.Bottom - 56;
            using (Font f = Sprites.Display(20))
            {
                for (int i = toasts.Count - 1; i >= 0 && i >= toasts.Count - 4; i--)
                {
                    Toast t = toasts[i];
                    Size size = TextRenderer.MeasureText(t.Text, f);
                    var box = new Rectangle(grid.Left + (grid.Width - size.Width - 28) / 2, y, size.Width + 28, 40);
                    g.FillRectangle(new SolidBrush(Ink), box.X + 5, box.Y + 5, box.Width, box.Height);
                    g.FillRectangle(new SolidBrush(t.Color), box);
                    g.DrawRectangle(new Pen(Ink, 3), box);
                    DrawCentered(g, t.Text, f, Ink, box, 0);
                    y -= 48;
                }
            }
        }

        // ===============================================================
        //  SMALL DRAWING HELPERS
        // ===============================================================
        public static void PaintHalftone(Graphics g, Rectangle r, Color background)
        {
            g.Clear(background);
            using (var dot = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                for (int y = 0; y < r.Height; y += 18)
                    for (int x = (y / 18) % 2 * 9; x < r.Width; x += 18)
                        g.FillEllipse(dot, x, y, 3, 3);
            }
        }

        public static void DrawCentered(Graphics g, string text, Font font, Color color, Rectangle box, int offsetY)
        {
            var r = new Rectangle(box.X, box.Y + offsetY, box.Width, box.Height);
            TextRenderer.DrawText(g, text, font, r, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
