// UIManager.cs - Member 3 (UI & Timers)
// Lahat ng screens ay Panel sa iisang window: Menu, HUD, Encounter at End.
// Binabasa lang ng UI ang engine. Hindi nito binabago ang puso o score.
namespace HandaKaNaBa
{
    public class UIManager
    {
        // Pop-art na kulay (ginagamit din ng MainForm)
        public static readonly Color Ink = Color.FromArgb(17, 17, 17), Cream = Color.FromArgb(255, 247, 214),
            Yellow = Color.FromArgb(253, 224, 71), Cyan = Color.FromArgb(34, 211, 238), Pink = Color.FromArgb(244, 114, 182);
        public const int PanicSeconds = 10;

        private Form form;
        private GameEngine engine;
        private Panel menuPanel, hudPanel, encounterPanel, endPanel;
        private Label lblHearts, lblStatus, lblTimer, lblWho, lblScenario, lblResult, lblEndTitle, lblStars;
        private Button[] cardButtons = new Button[3];
        private Button btnContinue;
        private TextBox txtReport;
        private System.Windows.Forms.Timer panicTimer = new System.Windows.Forms.Timer { Interval = 1000 };
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

            // nakikinig sa EVENTS ng engine
            engine.Changed += UpdateHud;
            engine.EncounterStarted += ShowEncounter;
            engine.Ended += ShowEnd;
            panicTimer.Tick += PanicTick;
            ShowOnly(menuPanel);
        }

        // ---------------- helpers para hindi paulit-ulit ----------------
        private Label MakeLabel(Control parent, string text, int x, int y, int w, int h, Font font, Color back)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = font,
                                  BackColor = back, ForeColor = Ink, TextAlign = ContentAlignment.MiddleCenter };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private Button MakeButton(Control parent, string text, int x, int y, int w, int h, Color back, Font font)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = font, BackColor = back,
                                   ForeColor = Ink, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabStop = false };
            btn.FlatAppearance.BorderSize = 4;          // makapal na itim na border = comic style
            btn.FlatAppearance.BorderColor = Ink;
            parent.Controls.Add(btn);
            return btn;
        }

        private Panel MakePanel(Color back, Point location, Size size, DockStyle dock)
        {
            var p = new Panel { BackColor = back, Location = location, Size = size, Dock = dock, Visible = false };
            p.Paint += Halftone;
            form.Controls.Add(p);
            return p;
        }

        private static Font Impact(float size) => new Font("Impact", size);

        // comic-style na tuldok sa background
        private void Halftone(object sender, PaintEventArgs e)
        {
            Control c = (Control)sender;
            using (var dot = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                for (int y = 0; y < c.Height; y += 18)
                    for (int x = (y / 18) % 2 * 9; x < c.Width; x += 18)
                        e.Graphics.FillEllipse(dot, x, y, 3, 3);
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
            menuPanel = MakePanel(Cyan, Point.Empty, Size.Empty, DockStyle.Fill);

            string[] names = { "TSUNAMI\n\nEASY", "LINDOL\n\nMEDIUM", "EL NIÑO\n\nHARD" };
            Color[] colors = { Color.FromArgb(96, 165, 250), Color.FromArgb(251, 146, 60), Color.FromArgb(202, 138, 4) };
            for (int i = 0; i < 3; i++)
            {
                Difficulty d = (Difficulty)i;
                MakeButton(menuPanel, names[i], 16 + i * 200, 230, 176, 180, colors[i], Impact(24)).Click += (s, e) => StartGame(d);
            }
            MakeLabel(menuPanel, "WASD / Arrows: gumalaw     1, 2, 3: pumili ng card\nSagipin sina Lola, Bunso at Bantay, tapos tumakbo sa EVAC!",
                      16, 450, 576, 70, Segoe(11), Color.White);
        }

        private void StartGame(Difficulty d)
        {
            engine.StartLevel(d);
            ShowOnly(hudPanel);
            UpdateHud();
            form.ActiveControl = null;   // para sa laro mapunta ang keyboard, hindi sa button
            form.Focus();
        }

        // ---------------- HUD ----------------
        private void BuildHud()
        {
            hudPanel = new Panel { BackColor = Cream, Dock = DockStyle.Top, Height = 64, Visible = false };
            form.Controls.Add(hudPanel);
            lblHearts = MakeLabel(hudPanel, "", 8, 10, 170, 44, new Font("Segoe UI Symbol", 22, FontStyle.Bold), Cream);
            lblHearts.ForeColor = Pink;
            lblStatus = MakeLabel(hudPanel, "", 184, 10, 416, 44, Impact(15), Yellow);
        }

        private void UpdateHud()
        {
            int lives = Math.Max(0, engine.Lives);
            lblHearts.Text = new string('♥', lives) + new string('♡', GameEngine.MaxLives - lives);
            lblStatus.Text = engine.Map.Current.Name + "    SAGIP " + engine.RescuedCount + "/3    "
                           + engine.Score + " PTS    x" + engine.Multiplier + "    " + engine.Seconds + "s";
        }

        public void Tick()
        {
            if (hudPanel.Visible) UpdateHud();
        }

        // ---------------- ENCOUNTER + PANIC TIMER ----------------
        private void BuildEncounter()
        {
            encounterPanel = MakePanel(Yellow, new Point(24, 80), new Size(560, 560), DockStyle.None);
            lblTimer = MakeLabel(encounterPanel, "10", 16, 12, 100, 90, Impact(54), Color.Transparent);
            lblWho = MakeLabel(encounterPanel, "", 124, 28, 420, 56, Impact(26), Color.White);
            lblScenario = MakeLabel(encounterPanel, "", 16, 112, 528, 104, Segoe(11), Ink);
            lblScenario.ForeColor = Color.White;
            lblScenario.Padding = new Padding(10);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                cardButtons[i] = MakeButton(encounterPanel, "", 16 + i * 182, 228, 164, 196, Color.White, Segoe(10));
                cardButtons[i].Click += (s, e) => Choose(index);
            }
            lblResult = MakeLabel(encounterPanel, "", 16, 436, 400, 110, Segoe(9.5f, FontStyle.Regular), Color.White);
            lblResult.TextAlign = ContentAlignment.TopLeft;
            btnContinue = MakeButton(encounterPanel, "TULOY ▶", 426, 452, 118, 76, Ink, Impact(18));
            btnContinue.ForeColor = Yellow;
            btnContinue.Click += (s, e) => Continue();
        }

        private void ShowEncounter()
        {
            picked = false;
            timeLeft = PanicSeconds;
            lblTimer.Text = timeLeft.ToString();
            lblTimer.ForeColor = Ink;
            lblWho.Text = "SAGIPIN SI " + engine.Target.Name.ToUpper() + "!";
            lblScenario.Text = engine.Scenario.Text;
            for (int i = 0; i < 3; i++)
            {
                Card c = engine.Scenario.Cards[i];
                cardButtons[i].Text = "[" + (i + 1) + "]\n\n" + c.Title + "\n\n" + c.Hint;
                cardButtons[i].BackColor = Color.White;
                cardButtons[i].FlatAppearance.BorderSize = 4;
            }
            lblResult.Visible = btnContinue.Visible = false;
            encounterPanel.Visible = true;
            encounterPanel.BringToFront();
            form.ActiveControl = null;
            panicTimer.Start();
        }

        private void PanicTick(object sender, EventArgs e)
        {
            timeLeft--;
            lblTimer.Text = timeLeft.ToString();
            if (timeLeft <= 3) lblTimer.ForeColor = Color.Red;
            if (timeLeft <= 0) Choose(-1);
        }

        private void Choose(int index)
        {
            if (picked) return;
            picked = true;
            panicTimer.Stop();
            engine.ChooseCard(index, Math.Max(0, timeLeft));

            // i-flip lahat ng 3 card para makita kung bakit tama o mali
            for (int i = 0; i < 3; i++)
            {
                Card c = engine.Scenario.Cards[i];
                cardButtons[i].Text = c.Label + "\n\n" + c.Title;
                cardButtons[i].BackColor = c.Color;
                cardButtons[i].FlatAppearance.BorderSize = i == index ? 8 : 2;
            }
            string reaction = index >= 0 ? engine.Scenario.Cards[index].Reaction : "Na-freeze ka! Naubos ang oras.";
            lblResult.Text = reaction + "\n\nAral: " + engine.Scenario.BestCard.Lesson + "\n" + engine.ResultText;
            lblResult.Visible = btnContinue.Visible = true;
        }

        private void Continue()
        {
            if (!picked) return;
            encounterPanel.Visible = false;
            engine.Resume();
            form.ActiveControl = null;
            form.Focus();
        }

        // keyboard habang may card: 1/2/3 pumili, Enter/Space tuloy
        public bool HandleKey(Keys key)
        {
            if (!encounterPanel.Visible) return false;
            if (!picked && key >= Keys.D1 && key <= Keys.D3) { Choose(key - Keys.D1); return true; }
            if (picked && (key == Keys.Enter || key == Keys.Space)) { Continue(); return true; }
            return false;
        }

        // ---------------- END SCREEN + REPORT ----------------
        private void BuildEnd()
        {
            endPanel = MakePanel(Yellow, Point.Empty, Size.Empty, DockStyle.Fill);
            lblEndTitle = MakeLabel(endPanel, "", 16, 20, 576, 76, Impact(44), Color.Transparent);
            lblStars = MakeLabel(endPanel, "", 16, 96, 576, 56, new Font("Segoe UI Symbol", 32, FontStyle.Bold), Color.Transparent);
            txtReport = new TextBox { Location = new Point(40, 160), Size = new Size(528, 400), Multiline = true, ReadOnly = true,
                                      ScrollBars = ScrollBars.Vertical, Font = Segoe(10.5f, FontStyle.Regular), TabStop = false };
            endPanel.Controls.Add(txtReport);
            MakeButton(endPanel, "ULITIN", 40, 576, 250, 64, Cyan, Impact(24)).Click += (s, e) => StartGame(engine.Disaster);
            MakeButton(endPanel, "MENU", 318, 576, 250, 64, Color.White, Impact(24)).Click += (s, e) => ShowOnly(menuPanel);
        }

        private void ShowEnd()
        {
            panicTimer.Stop();
            bool won = engine.State == GameState.Clear;
            endPanel.BackColor = won ? Yellow : Pink;
            lblEndTitle.Text = won ? "NAKALIGTAS!" : "SABLAY! UBOS ANG PUSO";
            lblStars.Text = won ? new string('★', engine.Stars) + new string('☆', 3 - engine.Stars) : "";

            var lines = new List<string>(engine.Summary) { "", "=== BARANGAY ASSEMBLY REPORT ===" };
            lines.Add(engine.Database.GetReport(engine.Disaster, engine.Stars));
            txtReport.Text = string.Join(Environment.NewLine, lines);
            ShowOnly(endPanel);
        }
    }
}
