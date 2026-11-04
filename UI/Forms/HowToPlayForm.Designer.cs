namespace HandaKaNaBa.UI.Forms
{
    partial class HowToPlayForm
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
            txtRules = new TextBox();
            btnClose = new Button();
            SuspendLayout();
            //
            // txtRules
            //
            txtRules.BackColor = Color.White;
            txtRules.Font = new Font("Segoe UI", 11F);
            txtRules.Location = new Point(15, 15);
            txtRules.Multiline = true;
            txtRules.Name = "txtRules";
            txtRules.ReadOnly = true;
            txtRules.ScrollBars = ScrollBars.Vertical;
            txtRules.Size = new Size(470, 330);
            txtRules.TabIndex = 0;
            txtRules.TabStop = false;
            //
            // btnClose
            //
            btnClose.Font = new Font("Segoe UI", 11F);
            btnClose.Location = new Point(185, 358);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(130, 40);
            btnClose.TabIndex = 1;
            btnClose.Text = "Got it!";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            //
            // HowToPlayForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 241, 222);
            ClientSize = new Size(500, 412);
            Controls.Add(txtRules);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "HowToPlayForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "How to Play";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtRules;
        private Button btnClose;
    }
}
