// Owner: Member 3
// Front end rule: this form only READS the engine's properties and calls PlayCard().
// It never changes lives or score directly.
using HandaKaNaBa.Engine;
using HandaKaNaBa.Models;
using HandaKaNaBa.Services;
using HandaKaNaBa.UI.Controls;

namespace HandaKaNaBa.UI.Forms
{
    public partial class GameForm : Form
    {
        private readonly IGameManager game;

        /// <summary>Set by the Game Over screen. MenuForm reads it to start a new game.</summary>
        public bool PlayAgainRequested { get; private set; }

        // Needed by the Visual Studio designer only
        public GameForm() : this(new FakeGameManager())
        {
        }

        public GameForm(IGameManager game)
        {
            InitializeComponent();
            this.game = game;
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            game.StartGame();
            game.StartRound();
            ShowRound();
        }

        /// <summary>Draws a fresh round from the engine state.</summary>
        private void ShowRound()
        {
            DisasterCard disaster = game.CurrentDisaster;

            BackColor = Theme.BackgroundFor(disaster.Type);
            lblScenario.ForeColor = Theme.ForDisaster(disaster.Type);
            lblScenario.Text = disaster.Emoji + " " + disaster.Name.ToUpper() + ": " + disaster.Scenario;

            panelScene.Sprite = AssetLoader.Scene(disaster.Type);
            panelScene.PlaceholderText = disaster.Name;
            panelReaction.Sprite = AssetLoader.Get(AssetLoader.FacePanic);
            panelResult.Sprite = null;
            panelResult.PlaceholderText = "?";

            BuildHand();
            RefreshStatus();

            lblMessage.Text = "Pick a card. Ano ang gagawin mo?";
            btnNext.Enabled = false;
            btnNext.Text = "Next ▶";
        }

        private void BuildHand()
        {
            flowHand.SuspendLayout();
            foreach (Control old in flowHand.Controls.Cast<Control>().ToList())
                old.Dispose();
            flowHand.Controls.Clear();

            for (int i = 0; i < game.Hand.Count; i++)
            {
                var cardPanel = new CardPanel(game.Hand[i], i);
                cardPanel.Click += Card_Click;
                flowHand.Controls.Add(cardPanel);
            }
            flowHand.ResumeLayout();
        }

        private void RefreshStatus()
        {
            int lives = Math.Max(0, game.Lives);
            lblHearts.Text = new string('♥', lives) + new string('♡', Math.Max(0, game.MaxLives - lives));
            lblStatus.Text = "Level " + game.Level + " | Round " + game.Round + "/" + game.RoundsPerLevel
                           + " | Score: " + game.Score;
        }

        private void Card_Click(object sender, EventArgs e)
        {
            var clicked = (CardPanel)sender;
            game.PlayCard(clicked.HandIndex);

            // Lock the hand until the player presses Next
            foreach (Control c in flowHand.Controls)
                c.Enabled = false;

            ShowResultPanels();
            lblMessage.Text = game.LastMessage;
            RefreshStatus();

            btnNext.Enabled = true;
            btnNext.Text = (game.IsGameOver || game.IsWin) ? "Results" : "Next ▶";
            btnNext.Focus();
        }

        /// <summary>Panels 2 and 3 change based on the pick.</summary>
        private void ShowResultPanels()
        {
            if (game.LastPlayed is MemeCard)
            {
                panelReaction.Sprite = AssetLoader.Get(AssetLoader.FaceMeme);
                panelResult.Sprite = AssetLoader.Get(AssetLoader.ResultFail);
                panelResult.PlaceholderText = "FAIL";
            }
            else if (game.LastResult == PickResult.Perfect)
            {
                panelReaction.Sprite = AssetLoader.Get(AssetLoader.FaceCalm);
                panelResult.Sprite = AssetLoader.Get(AssetLoader.ResultSafe);
                panelResult.PlaceholderText = "SAFE";
            }
            else if (game.LastResult == PickResult.Partial)
            {
                panelReaction.Sprite = AssetLoader.Get(AssetLoader.FacePanic);
                panelResult.Sprite = AssetLoader.Get(AssetLoader.ResultHurt);
                panelResult.PlaceholderText = "HURT";
            }
            else
            {
                panelReaction.Sprite = AssetLoader.Get(AssetLoader.FacePanic);
                panelResult.Sprite = AssetLoader.Get(AssetLoader.ResultFail);
                panelResult.PlaceholderText = "FAIL";
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (game.IsGameOver || game.IsWin)
            {
                EndGame();
                return;
            }

            game.StartRound();
            ShowRound();
        }

        private void EndGame()
        {
            bool newRecord = ScoreManager.SaveIfHigher(game.Score);
            using (var over = new GameOverForm(game.IsWin, game.Score, game.Level, newRecord))
            {
                over.ShowDialog(this);
                PlayAgainRequested = over.PlayAgainRequested;
            }
            Close();
        }
    }
}
