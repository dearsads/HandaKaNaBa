// Owner: Front End (Members 2 & 1)
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

        private static IGameManager CreateGame()
        {
            return new GameManager();
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
