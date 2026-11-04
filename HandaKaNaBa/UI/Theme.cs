// Owner: Front End (Members 2 & 1)
// Colors and fonts in one place so every screen matches.
using HandaKaNaBa.Models;

namespace HandaKaNaBa.UI
{
    public static class Theme
    {
        public static readonly Color CardFace = Color.FromArgb(255, 252, 245);
        public static readonly Color CardHeader = Color.FromArgb(38, 70, 83);   // same for every card so it does not give away the answer
        public static readonly Color CardBorder = Color.FromArgb(60, 60, 60);
        public static readonly Color CardHover = Color.FromArgb(233, 196, 106);
        public static readonly Color Heart = Color.FromArgb(214, 40, 57);
        public static readonly Color DarkText = Color.FromArgb(30, 30, 30);

        public static readonly Font TitleFont = new Font("Segoe UI", 28F, FontStyle.Bold);
        public static readonly Font EmojiFont = new Font("Segoe UI Emoji", 24F);
        public static readonly Font CardNameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font CardTextFont = new Font("Segoe UI", 8F);

        /// <summary>Strong color for each disaster (scenario text, accents).</summary>
        public static Color ForDisaster(DisasterType type)
        {
            switch (type)
            {
                case DisasterType.Flood:      return Color.FromArgb(29, 78, 137);
                case DisasterType.Fire:       return Color.FromArgb(200, 60, 30);
                case DisasterType.Typhoon:    return Color.FromArgb(70, 80, 100);
                case DisasterType.Earthquake: return Color.FromArgb(120, 72, 40);
                case DisasterType.Landslide:  return Color.FromArgb(90, 100, 40);
                default:                      return Color.FromArgb(190, 140, 20); // Drought
            }
        }

        /// <summary>Light tint of the disaster color for the form background.</summary>
        public static Color BackgroundFor(DisasterType type)
        {
            Color c = ForDisaster(type);
            return Color.FromArgb(
                c.R + (255 - c.R) * 85 / 100,
                c.G + (255 - c.G) * 85 / 100,
                c.B + (255 - c.B) * 85 / 100);
        }
    }
}
