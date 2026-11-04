// Owner: Front End (Members 2 & 1)
namespace HandaKaNaBa.UI.Forms
{
    public partial class HowToPlayForm : Form
    {
        public HowToPlayForm()
        {
            InitializeComponent();
            txtRules.Text = string.Join(Environment.NewLine, new[]
            {
                "HANDA KA NA BA?",
                "",
                "1. A disaster appears each round.",
                "2. Pick ONE card from your hand to protect the community.",
                "",
                "PERFECT - the right card for the disaster: +10 points",
                "PARTIAL - a general card (Go-Bag, First Aid Kit...): -1 heart, +5 points",
                "WRONG - a card for a different disaster: -2 hearts",
                "",
                "Every hand has one Meme card. It is funny, but it is always WRONG.",
                "",
                "Points are multiplied by the level.",
                "3 Perfects in a row = +1 heart.",
                "Higher levels = fewer cards and bigger damage.",
                "",
                "Survive 5 levels to save the community!",
            });
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
