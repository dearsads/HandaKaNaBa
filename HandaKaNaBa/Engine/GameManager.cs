// Owner: Back End (Members 1 & 2)
// COMPOSITION: GameManager owns the Deck and the Player.
// The UI only reads the properties below and calls StartGame(), StartRound() and PlayCard().
using HandaKaNaBa.Data;
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class GameManager : IGameManager
    {
        public const int MaxLevel = 5;
        private const int RoundsEachLevel = 5;

        private readonly Random random;
        private readonly Player player = new Player();
        private readonly List<DisasterCard> disasters = CardData.Disasters();
        private readonly List<MemeCard> memeCards = CardData.MemeCards();
        private Deck<ResponseCard> deck;
        private MemeCard currentMeme;

        public GameManager() : this(new Random())
        {
        }

        /// <summary>Pass a Random with a fixed seed to get the same game every time (useful for testing).</summary>
        public GameManager(Random random)
        {
            this.random = random;
        }

        // ----- Contract properties (the UI reads these) -----
        public int Level { get; private set; }
        public int Round { get; private set; }
        public int RoundsPerLevel => RoundsEachLevel;
        public int Lives => player.Lives;
        public int MaxLives => Player.MaxLives;
        public int Score => player.Score;
        public int Streak => player.Streak;
        public bool IsGameOver { get; private set; }
        public bool IsWin { get; private set; }
        public DisasterCard CurrentDisaster { get; private set; }
        public List<ResponseCard> Hand => player.Hand;
        public ResponseCard LastPlayed { get; private set; }
        public PickResult LastResult { get; private set; }
        public string LastMessage { get; private set; } = "";

        // ----- Rules tables (doc sections 3 and 4) -----

        /// <summary>Total hand size INCLUDING the Meme card. Levels 1-2: 5, Levels 3-4: 4, Level 5: 3.</summary>
        public static int GetHandSize(int level)
        {
            if (level <= 2) return 5;
            if (level <= 4) return 4;
            return 3;
        }

        /// <summary>Lives lost for a result at a given level.</summary>
        public static int GetDamage(PickResult result, int level)
        {
            switch (result)
            {
                case PickResult.Perfect: return 0;
                case PickResult.Partial: return level >= 5 ? 2 : 1;
                default:                 return level >= 3 ? 3 : 2;
            }
        }

        /// <summary>Base points before multiplying by the level.</summary>
        public static int GetPoints(PickResult result)
        {
            switch (result)
            {
                case PickResult.Perfect: return 10;
                case PickResult.Partial: return 5;
                default:                 return 0;
            }
        }

        // ----- Game flow -----

        public void StartGame()
        {
            player.Reset();
            Level = 1;
            Round = 0;
            IsGameOver = false;
            IsWin = false;
            CurrentDisaster = null;
            currentMeme = null;
            LastPlayed = null;
            LastMessage = "";

            deck = new Deck<ResponseCard>(CardData.ResponseCards(), random);
            deck.Shuffle();
        }

        /// <summary>Moves to the next round: new disaster, refilled hand, fairness rule, Meme card.</summary>
        public void StartRound()
        {
            if (deck == null)
                throw new InvalidOperationException("Call StartGame() before StartRound().");
            if (IsGameOver || IsWin)
                return;

            // 1. Advance the round and level
            Round++;
            if (Round > RoundsPerLevel)
            {
                Level++;
                Round = 1;
            }

            // 2. Random disaster, never the same as last round
            List<DisasterCard> choices = disasters
                .Where(d => CurrentDisaster == null || d.Type != CurrentDisaster.Type)
                .ToList();
            CurrentDisaster = choices[random.Next(choices.Count)];

            // 3. Remove last round's Meme card if it was not picked
            if (currentMeme != null)
            {
                Hand.Remove(currentMeme);
                currentMeme = null;
            }

            // 4. Resize the hand to (hand size - 1) normal cards
            int normalCards = GetHandSize(Level) - 1;
            while (Hand.Count > normalCards)
            {
                ResponseCard extra = Hand[Hand.Count - 1];
                Hand.RemoveAt(Hand.Count - 1);
                deck.Discard(extra);
            }
            while (Hand.Count < normalCards)
            {
                Hand.Add(deck.Draw());
            }

            // 5. Fairness rule: make sure at least one Perfect card is in the hand
            if (!Hand.Any(IsPerfectNow))
            {
                ResponseCard perfect = deck.TakeFirst(IsPerfectNow);
                if (perfect != null)
                {
                    int swapIndex = random.Next(Hand.Count);
                    deck.Discard(Hand[swapIndex]);
                    Hand[swapIndex] = perfect;
                }
            }

            // 6. Meme rule: add one Meme card for this disaster at a random position
            List<MemeCard> memes = memeCards.Where(m => m.ForDisaster == CurrentDisaster.Type).ToList();
            currentMeme = memes[random.Next(memes.Count)];
            Hand.Insert(random.Next(Hand.Count + 1), currentMeme);

            LastPlayed = null;
            LastMessage = "";
        }

        /// <summary>Evaluates the picked card and updates lives, score, streak and win/lose state.</summary>
        public PickResult PlayCard(int handIndex)
        {
            if (IsGameOver || IsWin)
                throw new InvalidOperationException("The game is already over.");
            if (handIndex < 0 || handIndex >= Hand.Count)
                throw new ArgumentOutOfRangeException(nameof(handIndex));

            ResponseCard card = Hand[handIndex];
            PickResult result = card.Evaluate(CurrentDisaster);   // POLYMORPHISM: each card type decides

            int damage = GetDamage(result, Level);
            int points = GetPoints(result) * Level;
            player.LoseLives(damage);
            player.AddScore(points);
            bool bonusLife = player.UpdateStreak(result);

            LastMessage = BuildMessage(card, result, damage, points, bonusLife);

            // The played card leaves the hand. Meme cards never go into the deck.
            Hand.RemoveAt(handIndex);
            if (card is MemeCard)
                currentMeme = null;
            else
                deck.Discard(card);

            LastPlayed = card;
            LastResult = result;

            if (player.Lives <= 0)
                IsGameOver = true;
            else if (Level == MaxLevel && Round == RoundsPerLevel)
                IsWin = true;

            return result;
        }

        private bool IsPerfectNow(ResponseCard card)
        {
            return card.Evaluate(CurrentDisaster) == PickResult.Perfect;
        }

        private string BuildMessage(ResponseCard card, PickResult result, int damage, int points, bool bonusLife)
        {
            string text;
            if (card is MemeCard meme)
                text = "WRONG! " + meme.FunnyLine + Environment.NewLine + "Lesson: " + meme.Lesson;
            else if (result == PickResult.Perfect)
                text = "PERFECT! " + card.Name + " is the right move.";
            else if (result == PickResult.Partial)
                text = "PARTIAL! " + card.Name + " helps, but it did not stop the " + CurrentDisaster.Name.ToLower() + ".";
            else
                text = "WRONG! " + card.Name + " is for a different disaster.";

            text += Environment.NewLine + "-" + damage + (damage == 1 ? " life" : " lives") + ", +" + points + " score";
            if (bonusLife)
                text += "   3 Perfects in a row: +1 life!";
            return text;
        }
    }
}
