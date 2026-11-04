// Owner: Member 2
// ENCAPSULATION: lives, score and streak are private. Other classes can only read them
// and change them through the public methods below.
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class Player
    {
        public const int MaxLives = 5;

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
            // TODO (Member 2): subtract amount from lives, but never go below 0
            throw new NotImplementedException("Player.LoseLives - Member 2");
        }

        public void AddScore(int points)
        {
            // TODO (Member 2): add points to score
            throw new NotImplementedException("Player.AddScore - Member 2");
        }

        /// <summary>Call after every pick. Returns true if this pick earned the streak bonus life.</summary>
        public bool UpdateStreak(PickResult result)
        {
            // TODO (Member 2):
            // Perfect -> streak++. If streak reaches 3: reset streak to 0, add 1 life (max MaxLives), return true
            // Partial or Wrong -> streak = 0
            throw new NotImplementedException("Player.UpdateStreak - Member 2");
        }
    }
}
