// Owner: Back End (Members 1 & 2)
// ENCAPSULATION: lives, score and streak are private. Other classes can only read them
// and change them through the public methods below.
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class Player
    {
        public const int MaxLives = 5;
        public const int StreakForBonus = 3;

        private int lives;
        private int score;
        private int streak;

        public int Lives => lives;
        public int Score => score;
        public int Streak => streak;
        public List<ResponseCard> Hand { get; } = new List<ResponseCard>();

        public Player()
        {
            Reset();
        }

        public void Reset()
        {
            lives = MaxLives;
            score = 0;
            streak = 0;
            Hand.Clear();
        }

        public void LoseLives(int amount)
        {
            if (amount < 0) amount = 0;
            lives = Math.Max(0, lives - amount);
        }

        public void AddScore(int points)
        {
            if (points > 0) score += points;
        }

        /// <summary>Call after every pick. Returns true if this pick earned the streak bonus life.</summary>
        public bool UpdateStreak(PickResult result)
        {
            if (result != PickResult.Perfect)
            {
                streak = 0;
                return false;
            }

            streak++;
            if (streak < StreakForBonus)
                return false;

            streak = 0;
            if (lives >= MaxLives)
                return false;   // already full, no bonus to give

            lives++;
            return true;
        }
    }
}
