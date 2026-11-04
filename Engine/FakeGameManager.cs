// TEMPORARY - lets the front end (Members 3 and 4) build and test the screens on Day 1
// before the real GameManager is ready. It uses simple rules: always 5 cards, no deck,
// no streak, fixed damage. Delete this file once GameManager works.
using HandaKaNaBa.Data;
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class FakeGameManager : IGameManager
    {
        private readonly Random random = new Random();
        private readonly List<DisasterCard> disasters = CardData.Disasters();
        private readonly List<ResponseCard> allCards = CardData.ResponseCards();
        private readonly List<MemeCard> memeCards = CardData.MemeCards();

        public int Level { get; private set; }
        public int Round { get; private set; }
        public int RoundsPerLevel => 5;
        public int Lives { get; private set; }
        public int MaxLives => 5;
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsWin { get; private set; }
        public DisasterCard CurrentDisaster { get; private set; }
        public List<ResponseCard> Hand { get; } = new List<ResponseCard>();
        public ResponseCard LastPlayed { get; private set; }
        public PickResult LastResult { get; private set; }
        public string LastMessage { get; private set; } = "";

        public void StartGame()
        {
            Level = 1;
            Round = 0;
            Lives = MaxLives;
            Score = 0;
            IsGameOver = false;
            IsWin = false;
            CurrentDisaster = null;
        }

        public void StartRound()
        {
            Round++;
            if (Round > RoundsPerLevel)
            {
                Level++;
                Round = 1;
            }

            // Random disaster, different from the last one
            DisasterCard next;
            do { next = disasters[random.Next(disasters.Count)]; }
            while (CurrentDisaster != null && next.Type == CurrentDisaster.Type);
            CurrentDisaster = next;

            // 1 correct card + 3 random cards + 1 meme card, shuffled
            Hand.Clear();
            var correct = allCards.Where(c => c.Evaluate(CurrentDisaster) == PickResult.Perfect).ToList();
            Hand.Add(correct[random.Next(correct.Count)]);
            foreach (var card in allCards.Where(c => !Hand.Contains(c)).OrderBy(c => random.Next()).Take(3))
                Hand.Add(card);
            var memes = memeCards.Where(m => m.ForDisaster == CurrentDisaster.Type).ToList();
            Hand.Add(memes[random.Next(memes.Count)]);

            var shuffled = Hand.OrderBy(c => random.Next()).ToList();
            Hand.Clear();
            Hand.AddRange(shuffled);

            LastPlayed = null;
            LastMessage = "";
        }

        public PickResult PlayCard(int handIndex)
        {
            ResponseCard card = Hand[handIndex];
            PickResult result = card.Evaluate(CurrentDisaster);

            int damage = result == PickResult.Perfect ? 0 : result == PickResult.Partial ? 1 : 2;
            int points = result == PickResult.Perfect ? 10 : result == PickResult.Partial ? 5 : 0;
            Lives = Math.Max(0, Lives - damage);
            Score += points * Level;

            if (card is MemeCard meme)
                LastMessage = "WRONG! " + meme.FunnyLine + Environment.NewLine + "Lesson: " + meme.Lesson;
            else if (result == PickResult.Perfect)
                LastMessage = "PERFECT! " + card.Name + " is the right move.";
            else if (result == PickResult.Partial)
                LastMessage = "PARTIAL! " + card.Name + " helps, but it did not stop the " + CurrentDisaster.Name.ToLower() + ".";
            else
                LastMessage = "WRONG! " + card.Name + " is for a different disaster.";

            LastMessage += "   (-" + damage + " lives, +" + (points * Level) + " score)";

            LastPlayed = card;
            LastResult = result;

            if (Lives == 0) IsGameOver = true;
            else if (Level == 5 && Round == RoundsPerLevel) IsWin = true;

            return result;
        }
    }
}
