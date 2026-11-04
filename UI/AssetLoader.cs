// Owner: Member 3 (images drawn by Member 4)
// Loads the pixel art from the Assets folder. Returns null if a file is missing,
// and PixelPanel then shows a placeholder instead of crashing.
using HandaKaNaBa.Models;

namespace HandaKaNaBa.UI
{
    public static class AssetLoader
    {
        public const string FaceCalm = "face_calm.png";
        public const string FacePanic = "face_panic.png";
        public const string FaceMeme = "face_meme.png";
        public const string ResultSafe = "result_safe.png";
        public const string ResultHurt = "result_hurt.png";
        public const string ResultFail = "result_fail.png";

        private static readonly Dictionary<string, Image> cache = new Dictionary<string, Image>();

        public static Image Get(string fileName)
        {
            if (cache.TryGetValue(fileName, out Image cached))
                return cached;

            Image image = null;
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
            if (File.Exists(path))
            {
                // Copy into a new Bitmap so the PNG file is not locked while the game runs
                using (Image fromFile = Image.FromFile(path))
                    image = new Bitmap(fromFile);
            }

            cache[fileName] = image;
            return image;
        }

        /// <summary>scene_flood.png, scene_fire.png, ...</summary>
        public static Image Scene(DisasterType type)
        {
            return Get("scene_" + type.ToString().ToLower() + ".png");
        }
    }
}
