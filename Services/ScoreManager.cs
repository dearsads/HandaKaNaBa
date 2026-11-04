// Owner: Member 4
// Saves the high score in a text file next to the .exe.
namespace HandaKaNaBa.Services
{
    public static class ScoreManager
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "highscore.txt");

        public static int LoadHighScore()
        {
            // TODO (Member 4):
            // if the file does not exist, return 0
            // otherwise read it with File.ReadAllText(FilePath) and int.TryParse it (return 0 if it fails)
            return 0;
        }

        /// <summary>Saves the score if it beats the current high score. Returns true if it is a new record.</summary>
        public static bool SaveIfHigher(int score)
        {
            // TODO (Member 4):
            // if score > LoadHighScore(): File.WriteAllText(FilePath, score.ToString()) and return true
            return false;
        }
    }
}
