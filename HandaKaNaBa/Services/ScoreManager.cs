// Owner: Front End (Members 2 & 1)
// Saves the high score in a text file next to the .exe.
namespace HandaKaNaBa.Services
{
    public static class ScoreManager
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "highscore.txt");

        public static int LoadHighScore()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return 0;

                return int.TryParse(File.ReadAllText(FilePath).Trim(), out int saved) ? saved : 0;
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
        }

        /// <summary>Saves the score if it beats the current high score. Returns true if it is a new record.</summary>
        public static bool SaveIfHigher(int score)
        {
            if (score <= LoadHighScore())
                return false;

            try
            {
                File.WriteAllText(FilePath, score.ToString());
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
