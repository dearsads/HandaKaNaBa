// Owner: Member 3
// Reusable card drawn with GDI+: colored header, emoji, name and description.
// The parent form handles Click and reads HandIndex.
using System.ComponentModel;
using System.Drawing.Drawing2D;
using HandaKaNaBa.Models;

namespace HandaKaNaBa.UI.Controls
{
    public class CardPanel : Control
    {
        private ResponseCard card;
        private bool hovering;

        public CardPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Size = new Size(140, 190);
            Margin = new Padding(6);
            Cursor = Cursors.Hand;
        }

        public CardPanel(ResponseCard card, int handIndex) : this()
        {
            Card = card;
            HandIndex = handIndex;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ResponseCard Card
        {
            get { return card; }
            set { card = value; Invalidate(); }
        }

        /// <summary>Position in the hand. This is what gets sent to PlayCard().</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int HandIndex { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovering = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovering = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : BackColor);

            Rectangle body = new Rectangle(2, 2, Width - 5, Height - 5);
            using (GraphicsPath path = RoundedRect(body, 10))
            {
                using (var fill = new SolidBrush(Theme.CardFace))
                    g.FillPath(fill, path);

                // Header strip
                g.SetClip(path);
                using (var header = new SolidBrush(Theme.CardHeader))
                    g.FillRectangle(header, body.X, body.Y, body.Width, 12);
                g.ResetClip();

                Color borderColor = hovering && Enabled ? Theme.CardHover : Theme.CardBorder;
                using (var pen = new Pen(borderColor, hovering && Enabled ? 4 : 2))
                    g.DrawPath(pen, path);
            }

            if (card == null)
                return;

            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(g, card.Emoji, Theme.EmojiFont,
                new Rectangle(0, 18, Width, 46), Theme.DarkText, flags | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, card.Name, Theme.CardNameFont,
                new Rectangle(8, 68, Width - 16, 50), Theme.DarkText, flags);
            TextRenderer.DrawText(g, card.Description, Theme.CardTextFont,
                new Rectangle(8, 120, Width - 16, Height - 126), Color.DimGray, flags);

            if (!Enabled)
            {
                using (var shade = new SolidBrush(Color.FromArgb(110, Color.White)))
                    g.FillRectangle(shade, ClientRectangle);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
