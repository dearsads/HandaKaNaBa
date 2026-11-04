// Owner: Front End (Members 2 & 1)
using HandaKaNaBa.Services;

namespace HandaKaNaBa.UI.Forms
{
    public partial class GameOverForm : Form
    {
        public bool PlayAgainRequested { get; private set; }

        // Needed by the Visual Studio designer only
        public GameOverForm() : this(false, 0, 1, false)
        {
        }

        public GameOverForm(bool isWin, int score, int levelReached, bool newHighScore)
        {
            InitializeComponent();

            if (isWin)
            {
                lblTitle.Text = "Community Saved! 🎉";
                lblTitle.ForeColor = Color.FromArgb(42, 157, 143);
            }
            else
            {
                lblTitle.Text = "Game Over";
                lblTitle.ForeColor = Color.FromArgb(214, 40, 57);
            }

            lblDetails.Text = "Final score: " + score + Environment.NewLine
                            + "Level reached: " + levelReached + Environment.NewLine
                            + (newHighScore ? "NEW HIGH SCORE!" : "High score: " + ScoreManager.LoadHighScore());
        }

        private void btnPlayAgain_Click(object sender, EventArgs e)
        {
            PlayAgainRequested = true;
            Close();
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            PlayAgainRequested = false;
            Close();
        }
    }
}
