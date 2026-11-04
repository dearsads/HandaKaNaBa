namespace HandaKaNaBa.UI.Forms
{
    partial class GameForm
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
            panelScene = new HandaKaNaBa.UI.Controls.PixelPanel();
            panelReaction = new HandaKaNaBa.UI.Controls.PixelPanel();
            panelResult = new HandaKaNaBa.UI.Controls.PixelPanel();
            lblHearts = new Label();
            lblStatus = new Label();
            lblScenario = new Label();
            flowHand = new FlowLayoutPanel();
            lblMessage = new Label();
            btnNext = new Button();
            SuspendLayout();
            //
            // panelScene  (Panel 1: disaster scene)
            //
            panelScene.Location = new Point(20, 12);
            panelScene.Name = "panelScene";
            panelScene.PlaceholderText = "scene";
            panelScene.Size = new Size(250, 200);
            panelScene.TabIndex = 0;
            //
            // panelReaction  (Panel 2: character reaction)
            //
            panelReaction.Location = new Point(285, 12);
            panelReaction.Name = "panelReaction";
            panelReaction.PlaceholderText = "reaction";
            panelReaction.Size = new Size(250, 200);
            panelReaction.TabIndex = 1;
            //
            // panelResult  (Panel 3: result)
            //
            panelResult.Location = new Point(550, 12);
            panelResult.Name = "panelResult";
            panelResult.PlaceholderText = "?";
            panelResult.Size = new Size(250, 200);
            panelResult.TabIndex = 2;
            //
            // lblHearts
            //
            lblHearts.Font = new Font("Segoe UI Symbol", 20F);
            lblHearts.ForeColor = Color.FromArgb(214, 40, 57);
            lblHearts.Location = new Point(16, 220);
            lblHearts.Name = "lblHearts";
            lblHearts.Size = new Size(300, 44);
            lblHearts.TabIndex = 3;
            lblHearts.Text = "♥♥♥♥♥";
            lblHearts.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblStatus
            //
            lblStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblStatus.Location = new Point(400, 220);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(400, 44);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Level 1 | Round 1/5 | Score: 0";
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            //
            // lblScenario
            //
            lblScenario.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblScenario.Location = new Point(20, 266);
            lblScenario.Name = "lblScenario";
            lblScenario.Size = new Size(780, 40);
            lblScenario.TabIndex = 5;
            lblScenario.Text = "SCENARIO: ...";
            lblScenario.TextAlign = ContentAlignment.MiddleLeft;
            //
            // flowHand  (the clickable cards are added here in code)
            //
            flowHand.Location = new Point(20, 310);
            flowHand.Name = "flowHand";
            flowHand.Size = new Size(780, 205);
            flowHand.TabIndex = 6;
            flowHand.WrapContents = false;
            //
            // lblMessage
            //
            lblMessage.BackColor = Color.White;
            lblMessage.BorderStyle = BorderStyle.FixedSingle;
            lblMessage.Font = new Font("Segoe UI", 10.5F);
            lblMessage.Location = new Point(20, 525);
            lblMessage.Name = "lblMessage";
            lblMessage.Padding = new Padding(8);
            lblMessage.Size = new Size(650, 95);
            lblMessage.TabIndex = 7;
            lblMessage.Text = "Pick a card.";
            //
            // btnNext
            //
            btnNext.Enabled = false;
            btnNext.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnNext.Location = new Point(685, 525);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(115, 95);
            btnNext.TabIndex = 8;
            btnNext.Text = "Next ▶";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            //
            // GameForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 635);
            Controls.Add(panelScene);
            Controls.Add(panelReaction);
            Controls.Add(panelResult);
            Controls.Add(lblHearts);
            Controls.Add(lblStatus);
            Controls.Add(lblScenario);
            Controls.Add(flowHand);
            Controls.Add(lblMessage);
            Controls.Add(btnNext);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "GameForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Handa Ka Na Ba?";
            Load += GameForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private HandaKaNaBa.UI.Controls.PixelPanel panelScene;
        private HandaKaNaBa.UI.Controls.PixelPanel panelReaction;
        private HandaKaNaBa.UI.Controls.PixelPanel panelResult;
        private Label lblHearts;
        private Label lblStatus;
        private Label lblScenario;
        private FlowLayoutPanel flowHand;
        private Label lblMessage;
        private Button btnNext;
    }
}
