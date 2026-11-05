// UIManager.cs - Members 3 and 4 (Screens, timer, menu, HUD, and interface polish).
// The UI presents the GameEngine state; gameplay rules remain inside GameEngine.
namespace HandaKaNaBa
{
    public class UIManager
    {
        // Shared pop-art palette. MainForm also uses Ink and Yellow when drawing the map.
        public static readonly Color Ink = Color.FromArgb(17, 17, 17);
        public static readonly Color Cream = Color.FromArgb(255, 247, 214);
        public static readonly Color Yellow = Color.FromArgb(253, 224, 71);
        public static readonly Color Cyan = Color.FromArgb(34, 211, 238);
        public static readonly Color Pink = Color.FromArgb(244, 114, 182);
        public static readonly Color Blue = Color.FromArgb(96, 165, 250);
        public static readonly Color Orange = Color.FromArgb(251, 146, 60);
        public static readonly Color Green = Color.FromArgb(52, 211, 153);

        public const int PanicSeconds = 20;
        private const int PanicWarningSeconds = 3;

        private readonly Form form;
        private readonly GameEngine engine;

        private Panel menuPanel, hudPanel, encounterPanel, endPanel;
        private Label lblHearts, lblStatus, lblTimer, lblWho, lblScenario;
        private Label lblResult, lblEndTitle, lblStars;
        private readonly Button[] cardButtons = new Button[3];
        private Button btnContinue;
        private TextBox txtReport;
        private Panel rescueProgressTrack, rescueProgressFill;

        private readonly System.Windows.Forms.Timer presentationTimer = new System.Windows.Forms.Timer
        {
            Interval = 110
        };

        private bool revealingCards;
        private bool revealingStars;
        private int cardRevealStep;
        private int selectedCardIndex = -1;
        private int starsRevealed;
        private int targetStars;
        private int encounterSlideStep = 4;
        private int previousLives = -1;
        private int previousScore = -1;
        private int heartPulseFrames;
        private int scorePulseFrames;

        private readonly System.Windows.Forms.Timer panicTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };

        private int timeLeft;
        private bool picked;

        public UIManager(Form form, GameEngine engine)
        {
            this.form = form ?? throw new ArgumentNullException(nameof(form));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

            BuildMenu();
            BuildHud();
            BuildEncounter();
            BuildEnd();

            // The engine remains the authority. The UI only reacts to its events.
            engine.Changed += UpdateHud;
            engine.EncounterStarted += ShowEncounter;
            engine.Ended += ShowEnd;
            panicTimer.Tick += PanicTick;
            presentationTimer.Tick += PresentationTick;

            ShowOnly(menuPanel);
        }

        // -----------------------------------------------------------------
        // Shared UI helpers
        // -----------------------------------------------------------------

        private Label MakeLabel(
            Control parent,
            string text,
            int x,
            int y,
            int width,
            int height,
            Font font,
            Color back)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = font,
                BackColor = back,
                ForeColor = Ink,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                UseMnemonic = false
            };

            parent.Controls.Add(label);
            return label;
        }

        private Button MakeButton(
            Control parent,
            string text,
            int x,
            int y,
            int width,
            int height,
            Color back,
            Font font)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = font,
                BackColor = back,
                ForeColor = Ink,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                TabStop = false,
                UseMnemonic = false,
                Padding = new Padding(6)
            };

            button.FlatAppearance.BorderSize = 4;
            button.FlatAppearance.BorderColor = Ink;
            button.FlatAppearance.MouseOverBackColor = AdjustColor(back, 18);
            button.FlatAppearance.MouseDownBackColor = AdjustColor(back, -22);

            parent.Controls.Add(button);
            return button;
        }

        private Panel MakePanel(Color back, Point location, Size size, DockStyle dock)
        {
            var panel = new Panel
            {
                BackColor = back,
                Location = location,
                Size = size,
                Dock = dock,
                Visible = false
            };

            panel.Paint += Halftone;
            form.Controls.Add(panel);
            return panel;
        }

        private static Color AdjustColor(Color color, int amount)
        {
            return Color.FromArgb(
                color.A,
                Math.Clamp(color.R + amount, 0, 255),
                Math.Clamp(color.G + amount, 0, 255),
                Math.Clamp(color.B + amount, 0, 255));
        }

        private static Font Impact(float size) => new Font("Impact", size);

        private static Font Segoe(float size, FontStyle style = FontStyle.Bold) =>
            new Font("Segoe UI", size, style);

        // Subtle comic-book halftone texture used by the menu, encounter, and end panels.
        private void Halftone(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            using (var dot = new SolidBrush(Color.FromArgb(38, Ink)))
            {
                for (int y = 0; y < control.Height; y += 18)
                {
                    int offset = (y / 18) % 2 == 0 ? 0 : 9;
                    for (int x = offset; x < control.Width; x += 18)
                        e.Graphics.FillEllipse(dot, x, y, 3, 3);
                }
            }
        }

        private void ShowOnly(Panel panel)
        {
            menuPanel.Visible = panel == menuPanel;
            hudPanel.Visible = panel == hudPanel;
            endPanel.Visible = panel == endPanel;
            encounterPanel.Visible = false;
            panel.BringToFront();
        }

        // -----------------------------------------------------------------
        // Main menu
        // -----------------------------------------------------------------

        private void BuildMenu()
        {
            menuPanel = MakePanel(Cyan, Point.Empty, Size.Empty, DockStyle.Fill);

            Label title = MakeLabel(
                menuPanel, "HANDA KA NA BA?",
                32, 48, 544, 92, Impact(46), Color.Transparent);
            title.ForeColor = Pink;

            Label subtitle = MakeLabel(
                menuPanel, "PUMILI NG SAKUNA",
                144, 154, 320, 40, Impact(19), Ink);
            subtitle.ForeColor = Color.White;

            string[] names =
            {
                "TSUNAMI\n\nEASY",
                "LINDOL\n\nMEDIUM",
                "EL NIÑO\n\nHARD"
            };

            Color[] colors = { Blue, Orange, Yellow };

            for (int i = 0; i < 3; i++)
            {
                Difficulty difficulty = (Difficulty)i;
                Button button = MakeButton(
                    menuPanel,
                    names[i],
                    16 + i * 200,
                    224,
                    176,
                    184,
                    colors[i],
                    Impact(25));

                button.Click += (sender, e) => StartGame(difficulty);
            }

            MakeLabel(
                menuPanel,
                "WASD / Arrows: gumalaw     1, 2, 3: pumili ng card\n" +
                "Sagipin sina Lola, Bunso at Bantay, tapos tumakbo sa EVAC!\n" +
                "TSUNAMI: umakyat o sumilong 1-3 tiles sa likod ng pader/burol sa hilera mo." +
                "BABALA: ang magkakadugtong na bahay ay may 2 alon lang bago gumuho.",
                16, 432, 576, 94, Segoe(10.5f), Color.Transparent);
        }

        private void StartGame(Difficulty difficulty)
        {
            panicTimer.Stop();
            presentationTimer.Stop();
            revealingCards = false;
            revealingStars = false;
            heartPulseFrames = 0;
            scorePulseFrames = 0;
            previousLives = -1;
            previousScore = -1;
            picked = false;

            engine.StartLevel(difficulty);
            ShowOnly(hudPanel);
            UpdateHud();

            // Keep keyboard input with the game instead of a menu button.
            form.ActiveControl = null;
            form.Focus();
        }

        // -----------------------------------------------------------------
        // Gameplay HUD
        // -----------------------------------------------------------------

        private void BuildHud()
        {
            hudPanel = MakePanel(
                Cream,
                Point.Empty,
                new Size(form.ClientSize.Width, 64),
                DockStyle.Top);
            hudPanel.Height = 64;

            lblHearts = MakeLabel(
                hudPanel, "",
                8, 7, 158, 50,
                new Font("Segoe UI Symbol", 23, FontStyle.Bold),
                Color.Transparent);
            lblHearts.ForeColor = Pink;

            lblStatus = MakeLabel(
                hudPanel, "",
                170, 5, 428, 38,
                Impact(14),
                Yellow);
            lblStatus.ForeColor = Ink;

            rescueProgressTrack = new Panel
            {
                Location = new Point(172, 48),
                Size = new Size(420, 7),
                BackColor = Color.FromArgb(65, Ink),
                BorderStyle = BorderStyle.FixedSingle
            };
            rescueProgressFill = new Panel
            {
                Location = Point.Empty,
                Size = new Size(0, 7),
                BackColor = Green
            };
            rescueProgressTrack.Controls.Add(rescueProgressFill);
            hudPanel.Controls.Add(rescueProgressTrack);
        }

        private void UpdateHud()
        {
            int lives = Math.Clamp(engine.Lives, 0, GameEngine.MaxLives);
            lblHearts.Text = new string('♥', lives) +
                             new string('♡', GameEngine.MaxLives - lives);

            string disasterName = engine.Map.Current?.Name ?? engine.Disaster.ToString().ToUpper();
            lblStatus.Text =
                disasterName + "  •  SAGIP " + engine.RescuedCount + "/3  •  " +
                engine.Score + " PTS  •  COMBO x" + engine.Multiplier + "  •  " +
                engine.Seconds + "s";

            rescueProgressFill.Width = (int)(420f * engine.RescuedCount / 3f);

            if (previousLives >= 0 && lives != previousLives)
                heartPulseFrames = 6;
            if (previousScore >= 0 && engine.Score != previousScore)
                scorePulseFrames = 6;

            previousLives = lives;
            previousScore = engine.Score;
            if (heartPulseFrames > 0 || scorePulseFrames > 0)
                StartPresentationTimer();
        }

        public void Tick()
        {
            if (hudPanel.Visible)
                UpdateHud();
        }

        // -----------------------------------------------------------------
        // Rescue encounter and 20-second decision timer
        // -----------------------------------------------------------------

        private void BuildEncounter()
        {
            encounterPanel = MakePanel(
                Yellow,
                new Point(24, 80),
                new Size(560, 560),
                DockStyle.None);

            lblTimer = MakeLabel(
                encounterPanel, PanicSeconds.ToString(),
                16, 12, 100, 90,
                Impact(54),
                Color.Transparent);

            lblWho = MakeLabel(
                encounterPanel, "",
                124, 25, 420, 62,
                Impact(26),
                Pink);

            lblScenario = MakeLabel(
                encounterPanel, "",
                16, 112, 528, 104,
                Segoe(11),
                Ink);
            lblScenario.ForeColor = Color.White;
            lblScenario.TextAlign = ContentAlignment.MiddleLeft;
            lblScenario.Padding = new Padding(12);

            for (int i = 0; i < 3; i++)
            {
                int cardIndex = i;
                cardButtons[i] = MakeButton(
                    encounterPanel,
                    "",
                    16 + i * 182,
                    228,
                    164,
                    196,
                    Color.White,
                    Segoe(10));

                cardButtons[i].Click += (sender, e) => Choose(cardIndex);
            }

            lblResult = MakeLabel(
                encounterPanel, "",
                16, 436, 400, 110,
                Segoe(9.5f, FontStyle.Regular),
                Color.Transparent);
            lblResult.TextAlign = ContentAlignment.TopLeft;
            lblResult.ForeColor = Ink;
            lblResult.Padding = new Padding(4, 2, 4, 2);

            btnContinue = MakeButton(
                encounterPanel,
                "TULOY ▶",
                426, 452, 118, 76,
                Ink,
                Impact(18));
            btnContinue.ForeColor = Yellow;
            btnContinue.Click += (sender, e) => Continue();
        }

        private void ShowEncounter()
        {
            picked = false;
            timeLeft = PanicSeconds;

            lblTimer.Text = timeLeft.ToString();
            lblTimer.ForeColor = Ink;
            lblTimer.BackColor = Color.Transparent;
            lblWho.Text = "SAGIPIN SI " + engine.Target.Name.ToUpper() + "!";
            lblScenario.Text = engine.Scenario.Text;

            for (int i = 0; i < 3; i++)
            {
                Card card = engine.Scenario.Cards[i];
                cardButtons[i].Text = "[" + (i + 1) + "]\n\n" +
                                      card.Title + "\n\n" + card.Hint;
                cardButtons[i].BackColor = Color.White;
                cardButtons[i].ForeColor = Ink;
                cardButtons[i].Enabled = true;
                cardButtons[i].FlatAppearance.BorderSize = 4;
            }

            lblResult.Visible = false;
            btnContinue.Visible = false;

            encounterPanel.Location = new Point(24, 100);
            encounterSlideStep = 0;
            encounterPanel.Visible = true;
            encounterPanel.BringToFront();
            form.ActiveControl = null;

            panicTimer.Stop();
            panicTimer.Start();
            StartPresentationTimer();
        }

        private void PanicTick(object sender, EventArgs e)
        {
            timeLeft = Math.Max(0, timeLeft - 1);
            lblTimer.Text = timeLeft.ToString();

            if (timeLeft <= PanicWarningSeconds)
            {
                lblTimer.ForeColor = Color.Red;
                lblTimer.BackColor = timeLeft % 2 == 0
                    ? Color.FromArgb(115, Pink)
                    : Color.Transparent;
            }

            if (timeLeft == 0)
                Choose(-1);
        }

        private void Choose(int index)
        {
            if (picked)
                return;

            picked = true;
            panicTimer.Stop();

            engine.ChooseCard(index, Math.Max(0, timeLeft));

            // Resolve the answer immediately, then reveal the three cards in sequence.
            selectedCardIndex = index;
            cardRevealStep = 0;
            revealingCards = true;
            lblResult.Visible = false;
            btnContinue.Visible = false;

            for (int i = 0; i < cardButtons.Length; i++)
            {
                cardButtons[i].Enabled = false;
                cardButtons[i].FlatAppearance.BorderSize = 4;
            }

            string reaction = index >= 0
                ? engine.Scenario.Cards[index].Reaction
                : "Na-freeze ka! Naubos ang oras.";

            lblResult.Text =
                reaction + "\n\nARAL: " + engine.Scenario.BestCard.Lesson +
                "\n\n" + engine.ResultText;

            StartPresentationTimer();
        }

        private void Continue()
        {
            if (!picked || !btnContinue.Visible || revealingCards)
                return;

            encounterPanel.Visible = false;
            engine.Resume();

            form.ActiveControl = null;
            form.Focus();
        }

        // While an encounter is open: 1-3 choose a card; Enter/Space continues.
        public bool HandleKey(Keys key)
        {
            if (!encounterPanel.Visible)
                return false;

            if (!picked)
            {
                if (key >= Keys.D1 && key <= Keys.D3)
                {
                    Choose(key - Keys.D1);
                    return true;
                }

                if (key >= Keys.NumPad1 && key <= Keys.NumPad3)
                {
                    Choose(key - Keys.NumPad1);
                    return true;
                }
            }
            else if (btnContinue.Visible && (key == Keys.Enter || key == Keys.Space))
            {
                Continue();
                return true;
            }

            return false;
        }

        private void StartPresentationTimer()
        {
            if (!presentationTimer.Enabled)
                presentationTimer.Start();
        }

        // Presentation-only animation: encounter panel slide, sequential card reveal,
        // heart/score feedback, and star-by-star end-screen reveal.
        private void PresentationTick(object sender, EventArgs e)
        {
            bool hasMoreAnimation = false;

            if (encounterPanel.Visible && encounterSlideStep < 4)
            {
                encounterSlideStep++;
                int y = 100 - (20 * encounterSlideStep / 4);
                encounterPanel.Location = new Point(24, y);
                hasMoreAnimation = true;
            }

            if (revealingCards)
            {
                if (cardRevealStep < cardButtons.Length)
                {
                    Card card = engine.Scenario.Cards[cardRevealStep];
                    Button button = cardButtons[cardRevealStep];
                    button.Text = card.Label + "\n\n" + card.Title;
                    button.BackColor = card.Color;
                    button.ForeColor = Ink;
                    button.FlatAppearance.BorderSize = cardRevealStep == selectedCardIndex ? 8 : 2;
                    cardRevealStep++;
                    hasMoreAnimation = true;
                }
                else
                {
                    revealingCards = false;
                    lblResult.Visible = true;
                    btnContinue.Visible = true;
                    encounterPanel.Invalidate();
                }
            }

            if (revealingStars)
            {
                starsRevealed++;
                lblStars.Text = new string('★', starsRevealed) + new string('☆', 3 - starsRevealed);
                if (starsRevealed >= targetStars)
                    revealingStars = false;
                else
                    hasMoreAnimation = true;
            }

            if (heartPulseFrames > 0)
            {
                lblHearts.ForeColor = heartPulseFrames % 2 == 0 ? Color.Red : Pink;
                heartPulseFrames--;
                hasMoreAnimation = true;
            }
            else
            {
                lblHearts.ForeColor = Pink;
            }

            if (scorePulseFrames > 0)
            {
                lblStatus.ForeColor = scorePulseFrames % 2 == 0 ? Green : Ink;
                scorePulseFrames--;
                hasMoreAnimation = true;
            }
            else
            {
                lblStatus.ForeColor = Ink;
            }

            if (!hasMoreAnimation && !revealingCards && !revealingStars &&
                heartPulseFrames <= 0 && scorePulseFrames <= 0)
                presentationTimer.Stop();
        }

        public void Dispose()
        {
            panicTimer.Stop();
            presentationTimer.Stop();
            engine.Changed -= UpdateHud;
            engine.EncounterStarted -= ShowEncounter;
            engine.Ended -= ShowEnd;
            panicTimer.Tick -= PanicTick;
            presentationTimer.Tick -= PresentationTick;
            panicTimer.Dispose();
            presentationTimer.Dispose();
        }

        // -----------------------------------------------------------------
        // End screen and Barangay Assembly Report
        // -----------------------------------------------------------------

        private void BuildEnd()
        {
            endPanel = MakePanel(Yellow, Point.Empty, Size.Empty, DockStyle.Fill);

            lblEndTitle = MakeLabel(
                endPanel, "",
                16, 20, 576, 76,
                Impact(44),
                Color.Transparent);

            lblStars = MakeLabel(
                endPanel, "",
                16, 96, 576, 56,
                new Font("Segoe UI Symbol", 32, FontStyle.Bold),
                Color.Transparent);
            lblStars.ForeColor = Ink;

            txtReport = new TextBox
            {
                Location = new Point(40, 160),
                Size = new Size(528, 400),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = Segoe(10.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Ink,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };
            endPanel.Controls.Add(txtReport);

            Button retryButton = MakeButton(
                endPanel, "ULITIN",
                40, 576, 250, 64,
                Cyan, Impact(24));
            retryButton.Click += (sender, e) => StartGame(engine.Disaster);

            Button menuButton = MakeButton(
                endPanel, "MENU",
                318, 576, 250, 64,
                Color.White, Impact(24));
            menuButton.Click += (sender, e) => ShowOnly(menuPanel);
        }

        private void ShowEnd()
        {
            panicTimer.Stop();

            bool won = engine.State == GameState.Clear;
            endPanel.BackColor = won ? Yellow : Pink;
            lblEndTitle.Text = won ? "NAKALIGTAS!" : "SABLAY! UBOS ANG PUSO";
            targetStars = won ? engine.Stars : 0;
            starsRevealed = 0;
            revealingStars = targetStars > 0;
            lblStars.Text = won ? new string('☆', 3) : "";
            encounterSlideStep = 4;
            if (revealingStars)
                StartPresentationTimer();

            var lines = new List<string>(engine.Summary)
            {
                "",
                "=== BARANGAY ASSEMBLY REPORT ==="
            };
            lines.Add(engine.Database.GetReport(engine.Disaster, engine.Stars));
            txtReport.Text = string.Join(Environment.NewLine, lines);

            ShowOnly(endPanel);
        }
    }
}
