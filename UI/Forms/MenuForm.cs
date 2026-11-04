// Owner: Member 4
// Flow: Menu -> Game -> Game Over -> (Play Again -> Game) or (Menu)
using HandaKaNaBa.Engine;
using HandaKaNaBa.Services;

namespace HandaKaNaBa.UI.Forms
{
    public partial class MenuForm : Form
    {
        public MenuForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// THE ONE-LINE SWAP: return new FakeGameManager() until the real engine works,
        /// then change it to new GameManager() on Day 2.
        /// </summary>
        private static IGameManager CreateGame()
        {
            return new FakeGameManager();
            // return new GameManager();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            Hide();
            bool playAgain;
            do
            {
                using (var gameForm = new GameForm(CreateGame()))
                {
                    gameForm.ShowDialog();
                    playAgain = gameForm.PlayAgainRequested;
                }
            } while (playAgain);
            Show();
        }

        private void btnHowToPlay_Click(object sender, EventArgs e)
        {
            using (var howTo = new HowToPlayForm())
                howTo.ShowDialog(this);
        }

        private void btnHighScore_Click(object sender, EventArgs e)
        {
            MessageBox.Show("High score: " + ScoreManager.LoadHighScore(), "High Score",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
