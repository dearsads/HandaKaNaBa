namespace HandaKaNaBa.UI.Forms
{
    partial class MenuForm
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
            lblSubtitle = new Label();
            btnPlay = new Button();
            btnHowToPlay = new Button();
            btnHighScore = new Button();
            btnExit = new Button();
            SuspendLayout();
            //
            // lblTitle
            //
            lblTitle.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(38, 70, 83);
            lblTitle.Location = new Point(20, 30);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(460, 60);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Handa Ka Na Ba?";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // lblSubtitle
            //
            lblSubtitle.Font = new Font("Segoe UI", 11F, FontStyle.Italic);
            lblSubtitle.Location = new Point(20, 92);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(460, 28);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "A disaster survival card game";
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // btnPlay
            //
            btnPlay.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            btnPlay.Location = new Point(140, 150);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(220, 50);
            btnPlay.TabIndex = 2;
            btnPlay.Text = "Play";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            //
            // btnHowToPlay
            //
            btnHowToPlay.Font = new Font("Segoe UI", 12F);
            btnHowToPlay.Location = new Point(140, 212);
            btnHowToPlay.Name = "btnHowToPlay";
            btnHowToPlay.Size = new Size(220, 45);
            btnHowToPlay.TabIndex = 3;
            btnHowToPlay.Text = "How to Play";
            btnHowToPlay.UseVisualStyleBackColor = true;
            btnHowToPlay.Click += btnHowToPlay_Click;
            //
            // btnHighScore
            //
            btnHighScore.Font = new Font("Segoe UI", 12F);
            btnHighScore.Location = new Point(140, 269);
            btnHighScore.Name = "btnHighScore";
            btnHighScore.Size = new Size(220, 45);
            btnHighScore.TabIndex = 4;
            btnHighScore.Text = "High Score";
            btnHighScore.UseVisualStyleBackColor = true;
            btnHighScore.Click += btnHighScore_Click;
            //
            // btnExit
            //
            btnExit.Font = new Font("Segoe UI", 12F);
            btnExit.Location = new Point(140, 326);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(220, 45);
            btnExit.TabIndex = 5;
            btnExit.Text = "Exit";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            //
            // MenuForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 241, 222);
            ClientSize = new Size(500, 410);
            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnPlay);
            Controls.Add(btnHowToPlay);
            Controls.Add(btnHighScore);
            Controls.Add(btnExit);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MenuForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Handa Ka Na Ba? - Menu";
            ResumeLayout(false);
        }

        #endregion

        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnPlay;
        private Button btnHowToPlay;
        private Button btnHighScore;
        private Button btnExit;
    }
}
