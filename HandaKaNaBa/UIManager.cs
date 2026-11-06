// UIManager.cs - Member 3 (Front-end: screens, HUD, encounter UI, timers, and animation)
// The UI only PRESENTS the GameEngine state. Gameplay rules stay inside GameEngine.
//
// File map (top to bottom):
//   1. UiStyle / UiText      - one place for sizes and for every word the player reads
//   2. Ease / Tweener / Fx   - tiny 60 FPS animation helpers (looks only, never rules)
//   3. PixelFonts / FitCache - Pixelify Sans loading and "shrink text to fit"
//   4. Pixel controls        - PixelLabel, PixelButton, PixelIconRow (hearts and stars)
//   5. UIManager             - menu, HUD, encounter panel, and end screen
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace HandaKaNaBa
{
    // =====================================================================
    //  1. STYLE AND WORDING (change sizes or language here, nowhere else)
    // =====================================================================

    internal static class UiStyle
    {
        // Shared geometry, so every panel, label and button looks related.
        public const int Depth = 6;        // hard drop-shadow / button depth
        public const int Border = 4;       // normal pixel border
        public const int BorderThin = 3;   // HUD cells
        public const int Frame = 6;        // frame around the encounter panel

        // One type scale (points). Every font in the game is one of these.
        public const float Small = 12f;    // HUD, cards, result, help
        public const float Body = 14f;     // scenario text, report
        public const float Label = 18f;    // TULOY button
        public const float Heading = 24f;  // menu, names, end buttons
        public const float Title = 36f;    // end-screen title
        public const float Display = 40f;  // big countdown number
    }

    internal static class UiText
    {
        // Menu
        public const string MenuHeading = "PUMILI NG SAKUNA";
        public static readonly string[] Disasters = { "TSUNAMI", "LINDOL", "EL NIÑO" };   // same order as Difficulty
        public static readonly string[] Tiers = { "EASY", "MEDIUM", "HARD" };
        public const string TsunamiShelter = "Sumilong 1-3 tiles sa likod ng pader o burol.";
        public const string Controls =
            "CONTROLS\n" +
            "WASD or Arrows: Move\n" +
            "1, 2, 3: Choose a card\n" +
            "Save your family, then evacuate!";

        // HUD
        public const string HudDisaster = "SAKUNA";
        public const string HudRescued = "SAGIP";
        public const string HudScore = "SCORE";
        public const string HudCombo = "COMBO";
        public const string HudTime = "ORAS";

        // Encounter
        public const string Rescue = "SAGIPIN SI ";
        public const string Continue = "TULOY";
        public const string Lesson = "ARAL: ";
        public const string TimeUp = "Na-freeze ka! Naubos ang oras.";

        // End screen
        public const string Win = "NAKALIGTAS!";
        public const string Lose = "SABLAY! UBOS ANG PUSO";
        public const string Retry = "UMULIT";
        public const string Menu = "MENU";
        public const string ReportHeader = "=== BARANGAY ASSEMBLY REPORT ===";
    }

    // =====================================================================
    //  2. ANIMATION HELPERS (presentation only - they never touch game rules)
    // =====================================================================

    internal static class Ease
    {
        public static double Linear(double t) => t;
        public static double InQuad(double t) => t * t;

        public static double OutCubic(double t)
        {
            double u = 1 - t;
            return 1 - u * u * u;
        }

        // Overshoots slightly, then settles: a springy arrival.
        public static double OutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            double u = t - 1;
            return 1 + c3 * u * u * u + c1 * u * u;
        }
    }

    // One shared ~60 FPS timer drives every animation, so they stay in sync.
    // The timer sleeps automatically when nothing is animating.
    internal static class Tweener
    {
        private sealed class Tween
        {
            public object Owner;
            public string Name;
            public double Begin, Delay, Duration;
            public Func<double, double> Ease;
            public Action<double> Apply;
            public Action Done;
        }

        private static readonly List<Tween> active = new List<Tween>();
        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static System.Windows.Forms.Timer timer;

        // Starting a tween with the same owner + name replaces the old one.
        public static void Run(
            object owner, string name, double durationMs,
            Func<double, double> ease, Action<double> apply,
            double delayMs = 0, Action done = null)
        {
            if (owner != null)
                active.RemoveAll(t => ReferenceEquals(t.Owner, owner) && t.Name == name);

            active.Add(new Tween
            {
                Owner = owner,
                Name = name,
                Begin = clock.Elapsed.TotalMilliseconds,
                Delay = delayMs,
                Duration = durationMs,
                Ease = ease ?? Ease.Linear,
                Apply = apply,
                Done = done
            });

            if (timer == null)
            {
                timer = new System.Windows.Forms.Timer { Interval = 15 };
                timer.Tick += Step;
            }
            if (!timer.Enabled)
                timer.Start();
        }

        public static void Cancel(object owner)
        {
            active.RemoveAll(t => ReferenceEquals(t.Owner, owner));
        }

        public static void Cancel(object owner, string name)
        {
            active.RemoveAll(t => ReferenceEquals(t.Owner, owner) && t.Name == name);
        }

        public static void Clear()
        {
            active.Clear();
            timer?.Stop();
        }

        private static void Step(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalMilliseconds;

            foreach (Tween t in active.ToArray())
            {
                if (!active.Contains(t))
                    continue;   // cancelled by an earlier callback this frame

                double elapsed = now - t.Begin - t.Delay;
                if (elapsed < 0)
                    continue;

                double p = t.Duration <= 0 ? 1.0 : Math.Min(1.0, elapsed / t.Duration);
                t.Apply?.Invoke(t.Ease(p));

                if (p >= 1.0)
                {
                    active.Remove(t);
                    t.Done?.Invoke();
                }
            }

            if (active.Count == 0)
                timer.Stop();
        }
    }

    // Reusable control effects built on Tweener.
    internal static class Fx
    {
        private static readonly Dictionary<Control, int> shakeHome = new Dictionary<Control, int>();

        public static Color Lerp(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        // Damped left-right wobble that always returns to the original position.
        public static void Shake(Control c, int px, double ms)
        {
            if (!shakeHome.ContainsKey(c))
                shakeHome[c] = c.Left;
            int home = shakeHome[c];

            Tweener.Run(c, "shake", ms, Ease.Linear,
                p => c.Left = home + (int)Math.Round(Math.Sin(p * Math.PI * 6) * px * (1 - p)),
                0,
                () => { c.Left = home; shakeHome.Remove(c); });
        }

        // Slides a control to toTop from fromOffset pixels away. While it waits for
        // its delay the control is hidden, so it never sits in the wrong place.
        public static void Slide(Control c, int toTop, int fromOffset, double ms, double delay, Func<double, double> ease)
        {
            c.Top = toTop + fromOffset;

            if (delay > 0)
            {
                c.Visible = false;
                Tweener.Run(c, "reveal", 0, Ease.Linear, null, delay, () => c.Visible = true);
            }

            Tweener.Run(c, "slide", ms, ease,
                p => c.Top = toTop + (int)Math.Round(fromOffset * (1 - p)),
                delay,
                () => c.Top = toTop);
        }

        public static void Cancel(Control c)
        {
            Tweener.Cancel(c);
            if (shakeHome.TryGetValue(c, out int home))
            {
                c.Left = home;
                shakeHome.Remove(c);
            }
        }

        public static void Reset()
        {
            foreach (var pair in shakeHome)
                pair.Key.Left = pair.Value;
            shakeHome.Clear();
            Tweener.Clear();
        }
    }

    // =====================================================================
    //  3. FONTS AND TEXT FITTING
    // =====================================================================

    internal static class PixelFonts
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);

        private const uint FR_PRIVATE = 0x10;

        private static PrivateFontCollection collection;
        private static FontFamily family;
        private static bool resolved;

        // Order: PixelifySans-*.ttf in Assets (no install needed) -> an installed
        // "Pixelify Sans" -> Consolas. AddFontResourceEx also shows the font to GDI,
        // so normal controls (the report TextBox) can use it too.
        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;

            try
            {
                string folder = Path.Combine(AppContext.BaseDirectory, "Assets");
                if (Directory.Exists(folder))
                {
                    collection = new PrivateFontCollection();
                    foreach (string file in Directory.GetFiles(folder, "PixelifySans-*.ttf"))
                    {
                        try
                        {
                            collection.AddFontFile(file);
                            AddFontResourceEx(file, FR_PRIVATE, IntPtr.Zero);
                        }
                        catch { }
                    }

                    family = collection.Families.FirstOrDefault(f => f.Name == "Pixelify Sans")
                             ?? collection.Families.FirstOrDefault();
                }
            }
            catch { }

            if (family == null)
            {
                try { family = new FontFamily("Pixelify Sans"); } catch { }
            }
            if (family == null)
            {
                try { family = new FontFamily("Consolas"); }
                catch { family = FontFamily.GenericMonospace; }
            }
        }

        public static Font Create(float size, FontStyle style = FontStyle.Bold)
        {
            Resolve();

            FontStyle use = style;
            if (!family.IsStyleAvailable(use)) use = FontStyle.Regular;
            if (!family.IsStyleAvailable(use)) use = FontStyle.Bold;

            return new Font(family, size, use, GraphicsUnit.Point);
        }
    }

    // Shrinks text in half-point steps until it fits its box, then remembers the answer.
    internal sealed class FitCache : IDisposable
    {
        private string key;
        private Font result;
        private bool owned;

        public Font Get(Graphics g, string text, Font baseFont, SizeF area, StringFormat sf, float minSize)
        {
            if (string.IsNullOrEmpty(text) || area.Width < 4 || area.Height < 4)
                return baseFont;

            string k = text + "\u0001" + baseFont.FontFamily.Name + baseFont.SizeInPoints +
                       (int)baseFont.Style + "|" + (int)area.Width + "x" + (int)area.Height;

            if (k == key && result != null)
                return result;

            Dispose();

            float size = baseFont.SizeInPoints;
            Font candidate = baseFont;
            bool created = false;

            while (size > minSize)
            {
                SizeF m = g.MeasureString(text, candidate, new SizeF(area.Width, 100000f), sf);
                if (m.Height <= area.Height + 0.5f)
                    break;

                size -= 0.5f;
                if (created) candidate.Dispose();
                candidate = new Font(baseFont.FontFamily, size, baseFont.Style, GraphicsUnit.Point);
                created = true;
            }

            result = candidate;
            owned = created;
            key = k;
            return result;
        }

        public void Dispose()
        {
            if (owned && result != null)
                result.Dispose();
            result = null;
            owned = false;
            key = null;
        }
    }

    // =====================================================================
    //  4. PIXEL CONTROLS
    // =====================================================================

    // Panel that repaints without flicker (used while panels slide).
    internal class FastPanel : Panel
    {
        public FastPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
        }
    }

    // A label drawn by hand: flat face, thick border, hard shadow, optional progress bar.
    public class PixelLabel : Label
    {
        private Color faceColor = Color.Transparent;
        private float textScale = 1f;
        private float progress = -1f;
        private readonly FitCache fit = new FitCache();

        public int BorderSize = 0;
        public bool Shadow = false;
        public bool TextShadow = false;

        // Optional text used only to size the font (so typing text out doesn't jitter).
        public string FitSource;
        public Color ProgressColor = Color.FromArgb(52, 211, 153);

        public Color FaceColor
        {
            get { return faceColor; }
            set { faceColor = value; Invalidate(); }
        }

        // 1 = normal. Animated for "pop" feedback.
        public float TextScale
        {
            get { return textScale; }
            set { textScale = value; Invalidate(); }
        }

        // 0..1 draws a bar along the bottom; negative hides it.
        public float Progress
        {
            get { return progress; }
            set { progress = value; Invalidate(); }
        }

        public PixelLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            BackColor = Color.Transparent;
        }

        // Brief scale bump: 1 -> 1+amount -> 1.
        public void Pulse(float amount = 0.2f, double ms = 240)
        {
            Tweener.Run(this, "pulse", ms, Ease.Linear,
                p => TextScale = 1f + amount * (float)Math.Sin(p * Math.PI),
                0,
                () => TextScale = 1f);
        }

        // Fades the text color from one color to another.
        public void Flash(Color from, Color to, double ms = 320)
        {
            ForeColor = from;
            Tweener.Run(this, "flash", ms, Ease.OutCubic,
                p => ForeColor = Fx.Lerp(from, to, p),
                0,
                () => ForeColor = to);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Fx.Cancel(this);
                fit.Dispose();
            }
            base.Dispose(disposing);
        }

        private static StringFormat FormatFor(ContentAlignment a)
        {
            var sf = new StringFormat(StringFormatFlags.NoClip)
            {
                HotkeyPrefix = HotkeyPrefix.None,
                Trimming = StringTrimming.None
            };

            switch (a)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    sf.LineAlignment = StringAlignment.Near; break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    sf.LineAlignment = StringAlignment.Far; break;
                default:
                    sf.LineAlignment = StringAlignment.Center; break;
            }

            switch (a)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    sf.Alignment = StringAlignment.Near; break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    sf.Alignment = StringAlignment.Far; break;
                default:
                    sf.Alignment = StringAlignment.Center; break;
            }

            return sf;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int s = Shadow ? UiStyle.Depth : 0;
            Rectangle face = new Rectangle(0, 0, Width - s, Height - s);
            if (face.Width <= 0 || face.Height <= 0)
                return;

            if (Shadow)
            {
                using (var b = new SolidBrush(UIManager.Ink))
                    g.FillRectangle(b, s, s, face.Width, face.Height);
            }

            if (faceColor != Color.Transparent)
            {
                using (var b = new SolidBrush(faceColor))
                    g.FillRectangle(b, face);
            }

            if (BorderSize > 0)
            {
                using (var b = new SolidBrush(UIManager.Ink))
                {
                    g.FillRectangle(b, face.X, face.Y, face.Width, BorderSize);
                    g.FillRectangle(b, face.X, face.Bottom - BorderSize, face.Width, BorderSize);
                    g.FillRectangle(b, face.X, face.Y, BorderSize, face.Height);
                    g.FillRectangle(b, face.Right - BorderSize, face.Y, BorderSize, face.Height);
                }
            }

            int barSpace = 0;
            if (progress >= 0f)
            {
                const int barHeight = 8;
                int inset = BorderSize + 4;
                var track = new Rectangle(
                    face.X + inset,
                    face.Bottom - BorderSize - 4 - barHeight,
                    face.Width - inset * 2,
                    barHeight);

                if (track.Width > 4)
                {
                    using (var b = new SolidBrush(UIManager.Ink))
                        g.FillRectangle(b, track);

                    int fill = (int)Math.Round((track.Width - 4) * Math.Max(0f, Math.Min(1f, progress)));
                    if (fill > 0)
                    {
                        using (var b = new SolidBrush(ProgressColor))
                            g.FillRectangle(b, track.X + 2, track.Y + 2, fill, track.Height - 4);
                    }
                }

                barSpace = barHeight + 6;
            }

            var textRect = new RectangleF(
                face.X + BorderSize + Padding.Left,
                face.Y + BorderSize + Padding.Top,
                face.Width - BorderSize * 2 - Padding.Horizontal,
                face.Height - BorderSize * 2 - Padding.Vertical - barSpace);

            if (string.IsNullOrEmpty(Text) || textRect.Width <= 0 || textRect.Height <= 0)
                return;

            GraphicsState state = g.Save();

            if (Math.Abs(textScale - 1f) > 0.001f)
            {
                float cx = textRect.X + textRect.Width / 2f;
                float cy = textRect.Y + textRect.Height / 2f;
                g.TranslateTransform(cx, cy);
                g.ScaleTransform(textScale, textScale);
                g.TranslateTransform(-cx, -cy);
            }

            using (var sf = FormatFor(TextAlign))
            {
                Font f = fit.Get(g, FitSource ?? Text, Font, textRect.Size, sf, 7f);

                if (TextShadow)
                {
                    var r2 = textRect;
                    r2.Offset(4, 4);
                    using (var b = new SolidBrush(UIManager.Ink))
                        g.DrawString(Text, f, b, r2, sf);
                }

                using (var b = new SolidBrush(ForeColor))
                    g.DrawString(Text, f, b, textRect, sf);
            }

            g.Restore(state);
        }
    }

    // A button drawn by hand: hard shadow, thick border, eased hover and press.
    public class PixelButton : Button
    {
        private Color faceColor = Color.White;
        private int borderThickness = UiStyle.Border;
        private float hoverT, pressT, dimT, sweepT = -1f, scaleX = 1f;
        private readonly FitCache fit = new FitCache();

        public Color FaceColor
        {
            get { return faceColor; }
            set { faceColor = value; Invalidate(); }
        }

        public int BorderThickness
        {
            get { return borderThickness; }
            set { borderThickness = value; Invalidate(); }
        }

        // Horizontal squash used for card flips and pop-in (0..1).
        public float ScaleX
        {
            get { return scaleX; }
            set { scaleX = value; Invalidate(); }
        }

        public PixelButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = false;
            Cursor = Cursors.Hand;
            UseMnemonic = false;
        }

        protected override bool ShowFocusCues { get { return false; } }

        public void PopIn(double delayMs = 0, double ms = 260)
        {
            ScaleX = 0f;
            Tweener.Run(this, "pop", ms, Ease.OutCubic,
                p => ScaleX = (float)p,
                delayMs,
                () => ScaleX = 1f);
        }

        // Dims the button a little (used on the other cards while one card is hovered).
        public void SetDim(float target)
        {
            float from = dimT;
            Tweener.Run(this, "dim", 140, Ease.OutCubic,
                p => { dimT = (float)(from + (target - from) * p); Invalidate(); });
        }

        // A bright stripe sweeps once across the face.
        private void StartShine()
        {
            sweepT = 0f;
            Tweener.Run(this, "shine", 420, Ease.OutCubic,
                p => { sweepT = (float)p; Invalidate(); },
                0,
                () => { sweepT = -1f; Invalidate(); });
        }

        private void TweenHover(float target)
        {
            float from = hoverT;
            Tweener.Run(this, "hover", 110, Ease.OutCubic,
                p => { hoverT = (float)(from + (target - from) * p); Invalidate(); });
        }

        private void TweenPress(float target)
        {
            float from = pressT;
            Tweener.Run(this, "press", 60, Ease.OutCubic,
                p => { pressT = (float)(from + (target - from) * p); Invalidate(); });
        }

        protected override void OnMouseEnter(EventArgs e) { TweenHover(1f); StartShine(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { TweenHover(0f); TweenPress(0f); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { TweenPress(1f); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { TweenPress(0f); base.OnMouseUp(e); }

        protected override void OnEnabledChanged(EventArgs e)
        {
            hoverT = 0f;
            pressT = 0f;
            sweepT = -1f;
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Tweener.Cancel(this);
                fit.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            float sx = Math.Max(0f, Math.Min(1f, scaleX));
            if (sx < 0.03f)
                return;

            GraphicsState state = g.Save();
            if (sx < 0.999f)
            {
                g.TranslateTransform(Width / 2f, 0);
                g.ScaleTransform(sx, 1f);
                g.TranslateTransform(-Width / 2f, 0);
            }

            // Resting buttons sit 2 px into their shadow. Hover lifts the face up-left
            // (bigger shadow); pressing pushes it down into the shadow.
            int depth = UiStyle.Depth;
            float lift = 2f * (1f - hoverT);
            int o = (int)Math.Round(lift + pressT * ((depth - 2) - lift));
            Rectangle face = new Rectangle(o, o, Width - depth, Height - depth);

            using (var b = new SolidBrush(UIManager.Ink))
                g.FillRectangle(b, depth, depth, Width - depth, Height - depth);

            // The face gets brighter as the mouse eases in.
            Color lit = ControlPaint.Light(faceColor, 0.35f);
            Color fill = Fx.Lerp(faceColor, lit, hoverT * (1f - pressT));
            fill = Fx.Lerp(fill, UIManager.Ink, 0.22f * dimT);   // other cards fade back

            using (var b = new SolidBrush(fill))
                g.FillRectangle(b, face);

            // The border thickens by up to 2 px on hover. The text box keeps the base
            // thickness, so the text never re-wraps while the border animates.
            int t = borderThickness;
            int td = t + (int)Math.Round(2f * hoverT);
            using (var b = new SolidBrush(UIManager.Ink))
            {
                g.FillRectangle(b, face.X, face.Y, face.Width, td);
                g.FillRectangle(b, face.X, face.Bottom - td, face.Width, td);
                g.FillRectangle(b, face.X, face.Y, td, face.Height);
                g.FillRectangle(b, face.Right - td, face.Y, td, face.Height);
            }

            if (sweepT >= 0f && sweepT <= 1f)
            {
                var inner = new Rectangle(face.X + td, face.Y + td, face.Width - td * 2, face.Height - td * 2);
                GraphicsState clipState = g.Save();
                g.SetClip(inner, CombineMode.Intersect);

                float skew = inner.Height * 0.45f;
                float x = inner.X - 50 + sweepT * (inner.Width + 100);
                PointF[] stripe =
                {
                    new PointF(x, inner.Bottom),
                    new PointF(x + 22, inner.Bottom),
                    new PointF(x + 22 + skew, inner.Top),
                    new PointF(x + skew, inner.Top)
                };

                using (var shine = new SolidBrush(Color.FromArgb(95, Color.White)))
                    g.FillPolygon(shine, stripe);

                g.Restore(clipState);
            }

            var textRect = new RectangleF(
                face.X + t + 2, face.Y + t + 2,
                face.Width - (t + 2) * 2, face.Height - (t + 2) * 2);

            if (!string.IsNullOrEmpty(Text) && textRect.Width > 0 && textRect.Height > 0)
            {
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    HotkeyPrefix = HotkeyPrefix.None,
                    Trimming = StringTrimming.None
                })
                using (var b = new SolidBrush(ForeColor))
                {
                    Font f = fit.Get(g, Text, Font, textRect.Size, sf, 7f);
                    g.DrawString(Text, f, b, textRect, sf);
                }
            }

            g.Restore(state);
        }
    }

    public enum IconKind { Heart, Star }

    // A row of hearts or stars drawn from small pixel grids (no font symbols needed).
    // The icon size is chosen automatically so the row always fits its box.
    public class PixelIconRow : Control
    {
        private static readonly string[] HeartShape = { "##.##", "#####", "#####", ".###.", "..#.." };
        private static readonly string[] StarShape =
            { "...#...", "..###..", "#######", ".#####.", "..###..", ".##.##.", ".#...#." };

        private readonly float[] pop = new float[8];
        private int total = 5, filled = 5;
        private float tint;

        public IconKind Kind = IconKind.Heart;
        public Color FaceColor = Color.Transparent;
        public int BorderSize = 0;
        public Color FilledColor = Color.FromArgb(247, 0, 99);
        public Color EmptyColor = Color.FromArgb(70, 70, 70);

        public int Total
        {
            get { return total; }
            set { total = Math.Max(0, Math.Min(pop.Length, value)); Invalidate(); }
        }

        public int Filled
        {
            get { return filled; }
            set { filled = value; Invalidate(); }
        }

        // 0..1 blends the filled color toward white (hit flash).
        public float Tint
        {
            get { return tint; }
            set { tint = value; Invalidate(); }
        }

        public PixelIconRow()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        // One icon grows and settles back.
        public void Pop(int index, double delayMs = 0)
        {
            if (index < 0 || index >= pop.Length)
                return;

            Tweener.Run(this, "pop" + index, 320, Ease.Linear,
                p => { pop[index] = (float)Math.Sin(p * Math.PI); Invalidate(); },
                delayMs,
                () => { pop[index] = 0f; Invalidate(); });
        }

        // The whole row flashes white, then returns to normal.
        public void FlashWhite(double ms = 380)
        {
            Tweener.Run(this, "tint", ms, Ease.OutCubic,
                p => Tint = (float)(1.0 - p),
                0,
                () => Tint = 0f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Fx.Cancel(this);
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var face = new Rectangle(0, 0, Width, Height);

            if (FaceColor != Color.Transparent)
            {
                using (var b = new SolidBrush(FaceColor))
                    g.FillRectangle(b, face);
            }

            if (BorderSize > 0)
            {
                using (var b = new SolidBrush(UIManager.Ink))
                {
                    g.FillRectangle(b, 0, 0, Width, BorderSize);
                    g.FillRectangle(b, 0, Height - BorderSize, Width, BorderSize);
                    g.FillRectangle(b, 0, 0, BorderSize, Height);
                    g.FillRectangle(b, Width - BorderSize, 0, BorderSize, Height);
                }
            }

            string[] shape = Kind == IconKind.Heart ? HeartShape : StarShape;
            int cols = shape[0].Length, rows = shape.Length;
            Rectangle inner = Rectangle.Inflate(face, -(BorderSize + 2), -(BorderSize + 2));
            if (total <= 0 || inner.Width <= 0 || inner.Height <= 0)
                return;

            // Pick the biggest pixel size where the whole row still fits.
            int unit = 1, outline = 1;
            for (int u = 8; u >= 1; u--)
            {
                int o = u >= 3 ? 2 : 1;
                int w = cols * u + 2 * o;
                int h = rows * u + 2 * o;
                if (total * w + (total - 1) * u <= inner.Width && h <= inner.Height)
                {
                    unit = u;
                    outline = o;
                    break;
                }
            }

            int iconW = cols * unit + 2 * outline;
            int iconH = rows * unit + 2 * outline;
            int gap = unit;
            int rowW = total * iconW + (total - 1) * gap;
            int startX = inner.X + (inner.Width - rowW) / 2;
            int startY = inner.Y + (inner.Height - iconH) / 2;

            // Little highlight pixel makes the icon look rounder.
            int hr = Kind == IconKind.Heart ? 1 : 2;
            int hc = Kind == IconKind.Heart ? 1 : 2;

            using (var ink = new SolidBrush(UIManager.Ink))
            using (var shine = new SolidBrush(Color.FromArgb(150, Color.White)))
            {
                for (int i = 0; i < total; i++)
                {
                    bool on = i < filled;
                    Color color = on ? Fx.Lerp(FilledColor, Color.White, tint) : EmptyColor;
                    int ix = startX + i * (iconW + gap);

                    GraphicsState state = g.Save();
                    float scale = 1f + 0.45f * pop[i];
                    if (scale > 1.001f)
                    {
                        float cx = ix + iconW / 2f;
                        float cy = startY + iconH / 2f;
                        g.TranslateTransform(cx, cy);
                        g.ScaleTransform(scale, scale);
                        g.TranslateTransform(-cx, -cy);
                    }

                    // Outline first (each cell grown by the outline width), then the colored cells.
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            if (shape[r][c] == '#')
                                g.FillRectangle(ink,
                                    ix + c * unit, startY + r * unit,
                                    unit + 2 * outline, unit + 2 * outline);

                    using (var fill = new SolidBrush(color))
                    {
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                if (shape[r][c] == '#')
                                    g.FillRectangle(fill,
                                        ix + outline + c * unit, startY + outline + r * unit,
                                        unit, unit);
                    }

                    if (on)
                        g.FillRectangle(shine,
                            ix + outline + hc * unit, startY + outline + hr * unit, unit, unit);

                    g.Restore(state);
                }
            }
        }
    }

    // =====================================================================
    //  5. UI MANAGER
    // =====================================================================

    public class UIManager
    {
        // Shared palette. MainForm also uses Ink, Yellow and Red when it draws.
        public static readonly Color Ink = Color.FromArgb(17, 17, 17);
        public static readonly Color Blue = ColorTranslator.FromHtml("#2391FF");
        public static readonly Color Yellow = ColorTranslator.FromHtml("#FFF83F");
        public static readonly Color Gold = ColorTranslator.FromHtml("#FFCD54");
        public static readonly Color Red = ColorTranslator.FromHtml("#F70063");
        public static readonly Color Green = Color.FromArgb(52, 211, 153);

        public const int PanicSeconds = 20;          // time to answer an encounter
        private const int PanicWarningSeconds = 3;   // timer turns red at this point

        private readonly Form form;
        private readonly GameEngine engine;

        private Panel menuPanel, hudPanel, encounterPanel, endPanel;
        private PixelIconRow lblHearts, lblStars;
        private PixelLabel lblName, lblSagip, lblScore, lblCombo, lblTime;
        private PixelLabel lblTimer, lblWho, lblFlavor, lblScenario, lblResult, lblEndTitle;
        private PixelLabel menuHeading, menuHelp;
        private PictureBox titleBox;
        private int titleTop;

        private readonly PixelButton[] menuButtons = new PixelButton[3];
        private readonly PixelButton[] cardButtons = new PixelButton[3];
        private PixelButton btnContinue, btnRetry, btnMenu;
        private TextBox txtReport;

        private readonly List<Font> fonts = new List<Font>();
        private readonly List<IDisposable> ownedImages = new List<IDisposable>();

        private readonly System.Windows.Forms.Timer panicTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };

        private int timeLeft;
        private bool picked;
        private bool revealingCards;
        private bool closingEncounter;
        private string resultText = "";

        // Remembered HUD values so animations only start on a real change.
        private int previousLives = -1;
        private int previousScore = -1;
        private int previousMultiplier = -1;
        private int previousRescued = -1;
        private double displayedScore;

        public UIManager(Form form, GameEngine engine)
        {
            this.form = form ?? throw new ArgumentNullException(nameof(form));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

            BuildMenu();
            BuildHud();
            BuildEncounter();
            BuildEnd();

            // The engine stays the authority. The UI only reacts to its events.
            engine.Changed += UpdateHud;
            engine.EncounterStarted += ShowEncounter;
            engine.Ended += ShowEnd;
            panicTimer.Tick += PanicTick;

            // Replay the menu intro every time the menu becomes visible.
            menuPanel.VisibleChanged += (s, e) =>
            {
                if (menuPanel.Visible)
                    PlayMenuIntro();
            };

            ShowOnly(menuPanel);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private Font Pixel(float size, FontStyle style = FontStyle.Bold)
        {
            Font f = PixelFonts.Create(size, style);
            fonts.Add(f);
            return f;
        }

        private PixelLabel MakeLabel(
            Control parent, string text,
            int x, int y, int w, int h,
            Font font, Color face,
            int border = 0, bool shadow = false)
        {
            var lbl = new PixelLabel
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = font,
                FaceColor = face,
                BorderSize = border,
                Shadow = shadow,
                ForeColor = Ink,
                TextAlign = ContentAlignment.MiddleCenter
            };

            parent.Controls.Add(lbl);
            return lbl;
        }

        private PixelButton MakeButton(
            Control parent, string text,
            int x, int y, int w, int h,
            Color face, Font font)
        {
            var btn = new PixelButton
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = font,
                FaceColor = face,
                ForeColor = Ink
            };

            parent.Controls.Add(btn);
            return btn;
        }

        private Panel MakePanel(Color back, Point location, Size size, DockStyle dock)
        {
            var panel = new FastPanel
            {
                BackColor = back,
                Location = location,
                Size = size,
                Dock = dock,
                Visible = false
            };

            form.Controls.Add(panel);
            return panel;
        }

        // Thick pixel frame around a panel.
        private static void AddFrame(Panel panel, int thickness)
        {
            panel.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Ink))
                {
                    Rectangle r = panel.ClientRectangle;
                    e.Graphics.FillRectangle(b, 0, 0, r.Width, thickness);
                    e.Graphics.FillRectangle(b, 0, r.Height - thickness, r.Width, thickness);
                    e.Graphics.FillRectangle(b, 0, 0, thickness, r.Height);
                    e.Graphics.FillRectangle(b, r.Width - thickness, 0, thickness, r.Height);
                }
            };
        }

        // Loads a PNG from Assets (copying it, so the file is not locked).
        // With crop = true, empty transparent margins are trimmed away.
        private Bitmap LoadBitmap(bool crop, params string[] fileNames)
        {
            foreach (string name in fileNames)
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", name);
                if (!File.Exists(path))
                    continue;

                Bitmap copy;
                using (Image image = Image.FromFile(path))
                    copy = new Bitmap(image);

                if (crop)
                {
                    Bitmap trimmed = CropToContent(copy);
                    if (!ReferenceEquals(trimmed, copy))
                    {
                        copy.Dispose();
                        copy = trimmed;
                    }
                }

                ownedImages.Add(copy);
                return copy;
            }
            return null;
        }

        // Finds the smallest rectangle that contains every visible pixel.
        private static Bitmap CropToContent(Bitmap source)
        {
            var whole = new Rectangle(0, 0, source.Width, source.Height);
            BitmapData data = source.LockBits(whole, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int minX = source.Width, minY = source.Height, maxX = -1, maxY = -1;

            try
            {
                byte[] pixels = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        if (pixels[y * data.Stride + x * 4 + 3] <= 8)
                            continue;   // transparent

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            finally
            {
                source.UnlockBits(data);
            }

            if (maxX < 0)
                return source;

            return source.Clone(
                new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1),
                PixelFormat.Format32bppArgb);
        }

        private void ShowOnly(Panel panel)
        {
            menuPanel.Visible = panel == menuPanel;
            hudPanel.Visible = panel == hudPanel;
            endPanel.Visible = panel == endPanel;
            encounterPanel.Visible = false;
            panel.BringToFront();
        }

        // -----------------------------------------------------------------
        // Main menu
        // -----------------------------------------------------------------

        private void BuildMenu()
        {
            menuPanel = MakePanel(Blue, Point.Empty, Size.Empty, DockStyle.Fill);

            Bitmap background = LoadBitmap(false, "menu-background.png");
            if (background != null)
            {
                menuPanel.BackgroundImage = background;
                menuPanel.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // The title PNG has a lot of empty space around the letters. Trimming it
            // lets the title use the whole banner area instead of a small centered square.
            // Both "menu_title.png" and "menu title.png" are accepted.
            Bitmap title = LoadBitmap(true, "menu_title.png", "menu title.png");
            if (title != null)
            {
                const int maxWidth = 560, maxHeight = 242;
                double scale = Math.Min((double)maxWidth / title.Width, (double)maxHeight / title.Height);
                int w = (int)Math.Round(title.Width * scale);
                int h = (int)Math.Round(title.Height * scale);

                var scaled = new Bitmap(w, h);
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(title, 0, 0, w, h);
                }

                ownedImages.Add(scaled);
                ownedImages.Remove(title);
                title.Dispose();

                titleTop = 8 + (maxHeight - h) / 2;
                titleBox = new PictureBox
                {
                    Size = new Size(w, h),
                    Location = new Point((form.ClientSize.Width - w) / 2, titleTop),
                    SizeMode = PictureBoxSizeMode.Normal,
                    BackColor = Color.Transparent,
                    Image = scaled
                };
                menuPanel.Controls.Add(titleBox);
            }

            menuHeading = MakeLabel(menuPanel, UiText.MenuHeading,
                84, 254, 440, 52, Pixel(UiStyle.Heading), Red, UiStyle.Border, true);
            menuHeading.ForeColor = Color.White;

            Color[] colors = { Yellow, Gold, Red };

            for (int i = 0; i < 3; i++)
            {
                Difficulty difficulty = (Difficulty)i;

                PixelButton btn = MakeButton(menuPanel,
                    UiText.Disasters[i] + "\n\n" + UiText.Tiers[i],
                    16 + i * 200, 320, 182, 170, colors[i], Pixel(UiStyle.Heading));

                if (i == 2)
                    btn.ForeColor = Color.White;

                btn.Click += (sender, e) => StartGame(difficulty);
                menuButtons[i] = btn;

                // Hovering a disaster previews its lesson (the same line the final report opens with).
                int tipIndex = i;
                btn.MouseEnter += (sender, e) => ShowDisasterTip(tipIndex);
                btn.MouseLeave += (sender, e) => ShowDisasterTip(-1);
            }

            menuHelp = MakeLabel(menuPanel, UiText.Controls,
                16, 510, 576, 96, Pixel(UiStyle.Small), Yellow, UiStyle.Border, true);
        }

        // Swaps the help panel between the controls and the hovered disaster's lesson.
        private void ShowDisasterTip(int index)
        {
            if (menuHelp == null)
                return;

            string text = UiText.Controls;
            if (index >= 0)
            {
                text = UiText.Disasters[index] + "\n" + EncounterDatabase.GetTip((Difficulty)index);
                if (index == (int)Difficulty.Tsunami)
                    text += "\n" + UiText.TsunamiShelter;
            }

            if (menuHelp.Text == text)
                return;

            menuHelp.Text = text;
            menuHelp.Pulse(0.06f, 200);
        }

        // Title drops in with a bounce, then the heading, buttons and help text follow.
        private void PlayMenuIntro()
        {
            ShowDisasterTip(-1);

            if (titleBox != null)
                Fx.Slide(titleBox, titleTop, -270, 560, 0, Ease.OutBack);

            Fx.Slide(menuHeading, 254, 36, 320, 220, Ease.OutCubic);

            for (int i = 0; i < menuButtons.Length; i++)
            {
                double delay = 300 + i * 90;
                Fx.Slide(menuButtons[i], 320, 80, 380, delay, Ease.OutBack);
                menuButtons[i].PopIn(delay, 260);
            }

            Fx.Slide(menuHelp, 510, 40, 320, 700, Ease.OutCubic);
        }

        private void StartGame(Difficulty difficulty)
        {
            panicTimer.Stop();
            Fx.Reset();   // stop every running animation and restore shaken controls

            revealingCards = false;
            closingEncounter = false;
            picked = false;
            previousLives = -1;
            previousScore = -1;
            previousMultiplier = -1;
            previousRescued = -1;

            hudPanel.BackColor = Gold;
            lblHearts.Tint = 0f;
            foreach (PixelLabel cell in new[] { lblName, lblSagip, lblScore, lblCombo, lblTime })
            {
                cell.TextScale = 1f;
                cell.ForeColor = Ink;
            }

            engine.StartLevel(difficulty);
            ShowOnly(hudPanel);
            UpdateHud();

            // Keep the keyboard with the game, not with a menu button.
            form.ActiveControl = null;
            form.Focus();
        }

        // -----------------------------------------------------------------
        // Gameplay HUD
        // -----------------------------------------------------------------

        private void BuildHud()
        {
            hudPanel = MakePanel(Gold, Point.Empty, new Size(form.ClientSize.Width, 64), DockStyle.Top);
            hudPanel.Height = 64;

            // Chunky bottom line.
            hudPanel.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Ink))
                    e.Graphics.FillRectangle(b, 0, hudPanel.Height - 4, hudPanel.Width, 4);
            };

            lblHearts = new PixelIconRow
            {
                Kind = IconKind.Heart,
                Location = new Point(8, 4),
                Size = new Size(118, 52),
                FaceColor = Ink,
                BorderSize = UiStyle.BorderThin,
                FilledColor = Red,
                EmptyColor = Color.FromArgb(74, 74, 82),
                Total = GameEngine.MaxLives,
                Filled = GameEngine.MaxLives
            };
            hudPanel.Controls.Add(lblHearts);

            lblName = MakeHudCell(134, 120);
            lblSagip = MakeHudCell(260, 80);
            lblSagip.Progress = 0f;   // rescue progress bar
            lblScore = MakeHudCell(346, 100);
            lblCombo = MakeHudCell(452, 62);
            lblTime = MakeHudCell(520, 72);
        }

        private PixelLabel MakeHudCell(int x, int w)
        {
            return MakeLabel(hudPanel, "", x, 4, w, 52, Pixel(UiStyle.Small), Yellow, UiStyle.BorderThin);
        }

        // Called by engine.Changed and on every game tick. Safe to call often:
        // an animation only starts when a value really changed.
        private void UpdateHud()
        {
            int lives = Math.Clamp(engine.Lives, 0, GameEngine.MaxLives);
            int rescued = engine.RescuedCount;
            int multiplier = engine.Multiplier;
            bool firstUpdate = previousLives < 0;

            lblHearts.Filled = lives;

            string disasterName = engine.Map.Current?.Name ?? engine.Disaster.ToString().ToUpper();
            lblName.Text = UiText.HudDisaster + "\n" + disasterName;
            lblSagip.Text = UiText.HudRescued + "\n" + rescued + "/3";
            lblCombo.Text = UiText.HudCombo + "\nx" + multiplier;
            lblTime.Text = UiText.HudTime + "\n" + engine.Seconds + "s";

            // Hearts: flash, shake and red HUD wash when hurt; a happy pop when healed.
            if (!firstUpdate && lives != previousLives)
            {
                if (lives < previousLives)
                {
                    lblHearts.FlashWhite(420);
                    Fx.Shake(lblHearts, 5, 340);
                    FlashHud();
                }
                else
                {
                    lblHearts.Pop(lives - 1);
                }
            }

            // Score counts up smoothly instead of jumping.
            if (previousScore < 0)
            {
                displayedScore = engine.Score;
                lblScore.Text = UiText.HudScore + "\n" + engine.Score;
            }
            else if (engine.Score != previousScore)
            {
                AnimateScore(engine.Score);
            }

            if (previousMultiplier >= 0 && multiplier != previousMultiplier)
            {
                lblCombo.Pulse(0.3f, 300);
                lblCombo.Flash(multiplier > previousMultiplier ? Red : Color.White, Ink, 380);
            }

            // The rescue bar glides to its new value.
            float targetProgress = rescued / 3f;
            if (previousRescued < 0)
            {
                lblSagip.Progress = targetProgress;
            }
            else if (rescued != previousRescued)
            {
                float from = Math.Max(0f, lblSagip.Progress);
                Tweener.Run(lblSagip, "progress", 420, Ease.OutCubic,
                    p => lblSagip.Progress = (float)(from + (targetProgress - from) * p),
                    0,
                    () => lblSagip.Progress = targetProgress);
                lblSagip.Pulse(0.25f, 320);
            }

            previousLives = lives;
            previousScore = engine.Score;
            previousMultiplier = multiplier;
            previousRescued = rescued;
        }

        private void AnimateScore(int target)
        {
            double from = displayedScore;

            Tweener.Run(lblScore, "count", 450, Ease.OutCubic,
                p =>
                {
                    displayedScore = from + (target - from) * p;
                    lblScore.Text = UiText.HudScore + "\n" + (int)Math.Round(displayedScore);
                },
                0,
                () =>
                {
                    displayedScore = target;
                    lblScore.Text = UiText.HudScore + "\n" + target;
                });

            lblScore.Pulse(0.25f, 320);
            lblScore.Flash(Green, Ink, 450);
        }

        // Quick red wash across the HUD when a life is lost.
        private void FlashHud()
        {
            hudPanel.BackColor = Red;
            Tweener.Run(hudPanel, "flash", 380, Ease.OutCubic,
                p => hudPanel.BackColor = Fx.Lerp(Red, Gold, p),
                0,
                () => hudPanel.BackColor = Gold);
        }

        public void Tick()
        {
            if (hudPanel.Visible)
                UpdateHud();
        }

        // -----------------------------------------------------------------
        // Rescue encounter and decision timer
        // -----------------------------------------------------------------

        private void BuildEncounter()
        {
            encounterPanel = MakePanel(Gold, new Point(24, 80), new Size(560, 560), DockStyle.None);
            AddFrame(encounterPanel, UiStyle.Frame);

            lblTimer = MakeLabel(encounterPanel, PanicSeconds.ToString(),
                20, 16, 104, 80, Pixel(UiStyle.Display), Yellow, UiStyle.Border, true);
            lblTimer.Progress = 1f;

            lblWho = MakeLabel(encounterPanel, "",
                136, 16, 404, 80, Pixel(UiStyle.Heading), Blue, UiStyle.Border, true);
            lblWho.ForeColor = Color.White;
            lblWho.TextShadow = true;

            // Scene-setting line. The final report quotes the same line back to the player.
            lblFlavor = MakeLabel(encounterPanel, "",
                20, 104, 520, 30, Pixel(UiStyle.Small), Color.White, UiStyle.BorderThin);
            lblFlavor.TextAlign = ContentAlignment.MiddleLeft;
            lblFlavor.Padding = new Padding(8, 0, 8, 0);

            lblScenario = MakeLabel(encounterPanel, "",
                20, 140, 520, 80, Pixel(UiStyle.Body), Ink, UiStyle.Border);
            lblScenario.ForeColor = Color.White;
            lblScenario.Padding = new Padding(8, 2, 8, 2);

            for (int i = 0; i < 3; i++)
            {
                int cardIndex = i;
                cardButtons[i] = MakeButton(encounterPanel, "",
                    20 + i * 180, 230, 168, 186, Color.White, Pixel(UiStyle.Small));
                cardButtons[i].Click += (sender, e) => Choose(cardIndex);

                // Hovering a card lifts it, thickens its border, sweeps a shine across it
                // and dims the other two cards.
                cardButtons[i].MouseEnter += (sender, e) => FocusCard(cardIndex);
                cardButtons[i].MouseLeave += (sender, e) => FocusCard(-1);
            }

            lblResult = MakeLabel(encounterPanel, "",
                20, 426, 396, 120, Pixel(UiStyle.Small), Color.White, UiStyle.Border);
            lblResult.TextAlign = ContentAlignment.TopLeft;
            lblResult.Padding = new Padding(6);

            btnContinue = MakeButton(encounterPanel, UiText.Continue,
                426, 436, 114, 100, Ink, Pixel(UiStyle.Label));
            btnContinue.ForeColor = Yellow;
            btnContinue.Click += (sender, e) => Continue();
        }

        private void ShowEncounter()
        {
            picked = false;
            revealingCards = false;
            closingEncounter = false;
            timeLeft = PanicSeconds;

            Fx.Cancel(encounterPanel);
            Fx.Cancel(lblTimer);
            Fx.Cancel(lblResult);
            Fx.Cancel(btnContinue);
            foreach (PixelButton card in cardButtons)
                Fx.Cancel(card);

            lblTimer.Text = timeLeft.ToString();
            lblTimer.ForeColor = Ink;
            lblTimer.FaceColor = Yellow;
            lblTimer.ProgressColor = Green;
            lblTimer.TextScale = 1f;
            lblTimer.Progress = 1f;

            lblWho.Text = UiText.Rescue + engine.Target.Name.ToUpper() + "!";
            lblFlavor.Text = engine.Scenario.Flavor;
            lblFlavor.Pulse(0.05f, 300);
            lblScenario.Text = engine.Scenario.Text;
            FocusCard(-1);

            for (int i = 0; i < 3; i++)
            {
                Card card = engine.Scenario.Cards[i];
                cardButtons[i].Text = "[" + (i + 1) + "]\n" + card.Title + "\n\n" + card.Hint;
                cardButtons[i].FaceColor = Color.White;
                cardButtons[i].BorderThickness = UiStyle.Border;
                cardButtons[i].ScaleX = 1f;
                cardButtons[i].Enabled = true;
            }

            lblResult.FitSource = null;
            lblResult.Visible = false;
            btnContinue.Visible = false;

            // The panel springs up, the header pops, and the cards arrive one by one.
            encounterPanel.Left = 24;
            Fx.Slide(encounterPanel, 80, 56, 340, 0, Ease.OutBack);
            encounterPanel.Visible = true;
            encounterPanel.BringToFront();

            lblWho.Pulse(0.08f, 320);

            for (int i = 0; i < cardButtons.Length; i++)
            {
                double delay = 140 + i * 70;
                Fx.Slide(cardButtons[i], 230, 28, 280, delay, Ease.OutCubic);
                cardButtons[i].PopIn(delay, 220);
            }

            // A smooth drain bar that matches the 1-second countdown.
            Tweener.Run(lblTimer, "drain", PanicSeconds * 1000.0, Ease.Linear,
                p => lblTimer.Progress = (float)(1.0 - p));

            form.ActiveControl = null;

            panicTimer.Stop();
            panicTimer.Start();
        }

        private void PanicTick(object sender, EventArgs e)
        {
            timeLeft = Math.Max(0, timeLeft - 1);
            lblTimer.Text = timeLeft.ToString();
            lblTimer.Pulse(0.2f, 260);

            if (timeLeft <= PanicWarningSeconds)
            {
                // Red warning state, blinking each second.
                lblTimer.ForeColor = Color.White;
                lblTimer.FaceColor = timeLeft % 2 == 0 ? Red : Color.FromArgb(190, 0, 80);
                lblTimer.ProgressColor = Color.White;
                Fx.Shake(lblTimer, 3, 260);
            }

            if (timeLeft == 0)
                Choose(-1);   // time ran out: the engine treats -1 as the default answer
        }

        private void Choose(int index)
        {
            if (picked)
                return;

            picked = true;
            panicTimer.Stop();
            Tweener.Cancel(lblTimer, "drain");
            FocusCard(-1);

            // The engine decides the result right away; the UI then plays the reveal.
            CardResult result = engine.ChooseCard(index, Math.Max(0, timeLeft));
            Scenario scenario = engine.Scenario;

            revealingCards = true;
            lblResult.Visible = false;
            btnContinue.Visible = false;

            foreach (PixelButton card in cardButtons)
                card.Enabled = false;

            string reaction = index >= 0
                ? scenario.Cards[index].Reaction
                : UiText.TimeUp;

            resultText = reaction + "\n\n" + UiText.Lesson + scenario.BestCard.Lesson + "\n" + engine.ResultText;

            // Wrong answers and timeouts shake the panel immediately.
            if (result == CardResult.Sablay)
                Fx.Shake(encounterPanel, 9, 380);

            // Flip the cards one after another to show their labels and colors.
            for (int i = 0; i < cardButtons.Length; i++)
                FlipCard(i, i * 130, scenario, index,
                    i == cardButtons.Length - 1 ? (Action)OnRevealFinished : null);
        }

        // Hover focus: the hovered card stays bright, the others fade back (-1 = none hovered).
        private void FocusCard(int index)
        {
            for (int i = 0; i < cardButtons.Length; i++)
                cardButtons[i].SetDim(index >= 0 && i != index ? 1f : 0f);
        }

        private void FlipCard(int i, double delay, Scenario scenario, int selected, Action finished)
        {
            PixelButton card = cardButtons[i];

            Tweener.Run(card, "flipOut", 90, Ease.InQuad,
                p => card.ScaleX = (float)(1.0 - p),
                delay,
                () =>
                {
                    Card data = scenario.Cards[i];
                    card.Text = data.Label + "\n\n" + data.Title;
                    card.FaceColor = data.Color;
                    card.BorderThickness = i == selected ? UiStyle.Border * 2 : UiStyle.BorderThin;

                    Tweener.Run(card, "flipIn", 140, Ease.OutCubic,
                        p => card.ScaleX = (float)p,
                        0,
                        () =>
                        {
                            card.ScaleX = 1f;
                            finished?.Invoke();
                        });
                });
        }

        private void OnRevealFinished()
        {
            revealingCards = false;
            if (closingEncounter)
                return;

            // Type the reaction and lesson out, then pop the TULOY button in.
            lblResult.FitSource = resultText;
            lblResult.Text = "";
            lblResult.Visible = true;

            double ms = Math.Min(1100.0, 14.0 * resultText.Length);
            Tweener.Run(lblResult, "type", ms, Ease.Linear,
                p => lblResult.Text = resultText.Substring(
                    0, Math.Min(resultText.Length, (int)(resultText.Length * p))),
                0,
                () => lblResult.Text = resultText);

            btnContinue.Visible = true;
            btnContinue.PopIn(100, 260);
        }

        private void Continue()
        {
            if (!picked || revealingCards || closingEncounter || !btnContinue.Visible)
                return;

            closingEncounter = true;
            Tweener.Cancel(lblResult, "type");
            lblResult.Text = resultText;

            engine.Resume();

            form.ActiveControl = null;
            form.Focus();

            // Gameplay resumes immediately while the panel slides away.
            if (encounterPanel.Visible)
            {
                int startTop = encounterPanel.Top;
                Tweener.Run(encounterPanel, "slide", 160, Ease.InQuad,
                    p => encounterPanel.Top = startTop + (int)Math.Round(70 * p),
                    0,
                    () =>
                    {
                        if (closingEncounter)
                            encounterPanel.Visible = false;
                    });
            }
        }

        // While an encounter is open: 1-3 choose a card; Enter/Space continues.
        public bool HandleKey(Keys key)
        {
            if (!encounterPanel.Visible)
                return false;

            if (!picked)
            {
                if (key >= Keys.D1 && key <= Keys.D3)
                {
                    Choose(key - Keys.D1);
                    return true;
                }

                if (key >= Keys.NumPad1 && key <= Keys.NumPad3)
                {
                    Choose(key - Keys.NumPad1);
                    return true;
                }
            }
            else if (btnContinue.Visible && (key == Keys.Enter || key == Keys.Space))
            {
                Continue();
                return true;
            }

            return false;
        }

        // -----------------------------------------------------------------
        // End screen and Barangay Assembly Report
        // -----------------------------------------------------------------

        private void BuildEnd()
        {
            endPanel = MakePanel(Gold, Point.Empty, Size.Empty, DockStyle.Fill);

            lblEndTitle = MakeLabel(endPanel, "", 16, 12, 576, 84,
                Pixel(UiStyle.Title), Color.Transparent);
            lblEndTitle.TextShadow = true;
            lblEndTitle.ForeColor = Color.White;

            lblStars = new PixelIconRow
            {
                Kind = IconKind.Star,
                Location = new Point(16, 98),
                Size = new Size(576, 56),
                FilledColor = Yellow,
                EmptyColor = Color.FromArgb(150, 105, 25),
                Total = 3,
                Filled = 0
            };
            endPanel.Controls.Add(lblStars);

            txtReport = new TextBox
            {
                Location = new Point(40, 160),
                Size = new Size(528, 400),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = Pixel(UiStyle.Body, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Ink,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };
            endPanel.Controls.Add(txtReport);

            btnRetry = MakeButton(endPanel, UiText.Retry, 40, 576, 250, 64, Yellow, Pixel(UiStyle.Heading));
            btnRetry.Click += (sender, e) => StartGame(engine.Disaster);

            btnMenu = MakeButton(endPanel, UiText.Menu, 318, 576, 250, 64, Color.White, Pixel(UiStyle.Heading));
            btnMenu.Click += (sender, e) => ShowOnly(menuPanel);
        }

        private void ShowEnd()
        {
            panicTimer.Stop();
            Tweener.Cancel(lblTimer, "drain");

            bool won = engine.State == GameState.Clear;
            int targetStars = won ? engine.Stars : 0;

            endPanel.BackColor = won ? Gold : Red;
            lblEndTitle.Text = won ? UiText.Win : UiText.Lose;
            lblStars.Visible = won;
            lblStars.Filled = 0;
            Fx.Cancel(lblStars);

            var lines = new List<string>(engine.Summary)
            {
                "",
                UiText.ReportHeader
            };
            lines.Add(engine.Database.GetReport(engine.Disaster, engine.Stars));
            txtReport.Text = string.Join(Environment.NewLine, lines);

            ShowOnly(endPanel);

            // The title drops in, stars pop one at a time, buttons rise from below.
            Fx.Slide(lblEndTitle, 12, -100, 520, 0, Ease.OutBack);

            for (int s = 1; s <= targetStars; s++)
            {
                int count = s;
                Tweener.Run(lblStars, "star" + s, 0, Ease.Linear, null,
                    450 + (s - 1) * 320,
                    () =>
                    {
                        lblStars.Filled = count;
                        lblStars.Pop(count - 1);
                    });
            }

            Fx.Slide(btnRetry, 576, 70, 340, 500, Ease.OutBack);
            btnRetry.PopIn(500, 240);
            Fx.Slide(btnMenu, 576, 70, 340, 600, Ease.OutBack);
            btnMenu.PopIn(600, 240);
        }

        // -----------------------------------------------------------------
        // Cleanup
        // -----------------------------------------------------------------

        public void Dispose()
        {
            panicTimer.Stop();
            Fx.Reset();

            engine.Changed -= UpdateHud;
            engine.EncounterStarted -= ShowEncounter;
            engine.Ended -= ShowEnd;
            panicTimer.Tick -= PanicTick;
            panicTimer.Dispose();

            foreach (Font f in fonts)
                f.Dispose();
            fonts.Clear();

            foreach (IDisposable image in ownedImages)
                image.Dispose();
            ownedImages.Clear();
        }
    }
}
