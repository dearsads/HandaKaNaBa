// Owner: Front End (Members 2 & 1)
// One comic panel. Draws a small pixel-art sprite scaled up with nearest-neighbor
// so the pixels stay sharp (a normal PictureBox would blur them).
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HandaKaNaBa.UI.Controls
{
    public class PixelPanel : Control
    {
        private Image sprite;
        private string placeholderText = "?";

        public PixelPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            BackColor = Color.White;
            Size = new Size(240, 200);
        }

        /// <summary>The image to show. Set to null to show the placeholder text.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image Sprite
        {
            get { return sprite; }
            set { sprite = value; Invalidate(); }
        }

        /// <summary>Shown when Sprite is null (for example "?" before the player picks).</summary>
        [DefaultValue("?")]
        public string PlaceholderText
        {
            get { return placeholderText; }
            set { placeholderText = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (sprite != null)
            {
                // Largest whole-number scale that fits, so every pixel becomes an even square
                int scale = Math.Max(1, Math.Min((Width - 8) / sprite.Width, (Height - 8) / sprite.Height));
                int w = sprite.Width * scale;
                int h = sprite.Height * scale;
                var dest = new Rectangle((Width - w) / 2, (Height - h) / 2, w, h);

                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(sprite, dest);
            }
            else
            {
                using (var pen = new Pen(Color.Gray, 2) { DashStyle = DashStyle.Dash })
                    g.DrawRectangle(pen, 6, 6, Width - 13, Height - 13);
                TextRenderer.DrawText(g, placeholderText, Font, ClientRectangle, Color.Gray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }

            using (var border = new Pen(Theme.CardBorder, 3))
                g.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
        }
    }
}
