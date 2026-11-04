namespace HandaKaNaBa.UI.Forms
{
    partial class GameOverForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblDetails = new Label();
            btnPlayAgain = new Button();
            btnMenu = new Button();
            SuspendLayout();
            //
            // lblTitle
            //
            lblTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            lblTitle.Location = new Point(20, 25);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(420, 50);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Game Over";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // lblDetails
            //
            lblDetails.Font = new Font("Segoe UI", 12F);
            lblDetails.Location = new Point(20, 85);
            lblDetails.Name = "lblDetails";
            lblDetails.Size = new Size(420, 90);
            lblDetails.TabIndex = 1;
            lblDetails.Text = "Final score: 0\r\nLevel reached: 1";
            lblDetails.TextAlign = ContentAlignment.MiddleCenter;
            //
            // btnPlayAgain
            //
            btnPlayAgain.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnPlayAgain.Location = new Point(50, 195);
            btnPlayAgain.Name = "btnPlayAgain";
            btnPlayAgain.Size = new Size(170, 48);
            btnPlayAgain.TabIndex = 2;
            btnPlayAgain.Text = "Play Again";
            btnPlayAgain.UseVisualStyleBackColor = true;
            btnPlayAgain.Click += btnPlayAgain_Click;
            //
            // btnMenu
            //
            btnMenu.Font = new Font("Segoe UI", 12F);
            btnMenu.Location = new Point(240, 195);
            btnMenu.Name = "btnMenu";
            btnMenu.Size = new Size(170, 48);
            btnMenu.TabIndex = 3;
            btnMenu.Text = "Main Menu";
            btnMenu.UseVisualStyleBackColor = true;
            btnMenu.Click += btnMenu_Click;
            //
            // GameOverForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 241, 222);
            ClientSize = new Size(460, 270);
            Controls.Add(lblTitle);
            Controls.Add(lblDetails);
            Controls.Add(btnPlayAgain);
            Controls.Add(btnMenu);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GameOverForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Handa Ka Na Ba?";
            ResumeLayout(false);
        }

        #endregion

        private Label lblTitle;
        private Label lblDetails;
        private Button btnPlayAgain;
        private Button btnMenu;
    }
}
