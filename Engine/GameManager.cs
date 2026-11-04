// Owner: Member 2
// COMPOSITION: GameManager owns the Deck and the Player.
// Done when: a test can play 25 rounds with random picks and reach Game Over or "Community Saved!".
// Until this is finished, the UI runs on FakeGameManager.
using HandaKaNaBa.Data;
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class GameManager : IGameManager
    {
        public const int MaxLevel = 5;
        private const int RoundsEachLevel = 5;

        private readonly Random random = new Random();
        private readonly Player player = new Player();
        private readonly List<DisasterCard> disasters = CardData.Disasters();
        private readonly List<MemeCard> memeCards = CardData.MemeCards();
        private Deck<ResponseCard> deck;
        private MemeCard currentMeme;

        // ----- Contract properties (the UI reads these) -----
        public int Level { get; private set; }
        public int Round { get; private set; }
        public int RoundsPerLevel => RoundsEachLevel;
        public int Lives => player.Lives;
        public int MaxLives => Player.MaxLives;
        public int Score => player.Score;
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
            // TODO (Member 2):
            // 1. player.Reset(); Level = 1; Round = 0; IsGameOver = false; IsWin = false
            // 2. deck = new Deck<ResponseCard>(CardData.ResponseCards(), random); deck.Shuffle()
            // 3. CurrentDisaster = null; LastPlayed = null; LastMessage = ""
            throw new NotImplementedException("GameManager.StartGame - Member 2");
        }

        public void StartRound()
        {
            // TODO (Member 2):
            // 1. Round++. If Round > RoundsPerLevel: Level++, Round = 1.
            //    (If Level > MaxLevel the game was already won in PlayCard.)
            // 2. Pick a random disaster that is NOT the same as the previous CurrentDisaster.
            // 3. Remove last round's Meme card from Hand (if any).
            // 4. Fill Hand with deck.Draw() until it has (GetHandSize(Level) - 1) normal cards.
            //    If the hand is too big (level went up), discard extra cards back to the deck.
            // 5. FAIRNESS RULE: if no card in Hand returns Perfect for CurrentDisaster,
            //    discard a random card and add deck.TakeFirst(c => c.Evaluate(CurrentDisaster) == PickResult.Perfect).
            // 6. MEME RULE: pick a random MemeCard where ForDisaster == CurrentDisaster.Type,
            //    save it in currentMeme, and insert it at a random position in Hand.
            // 7. LastPlayed = null; LastMessage = "";
            throw new NotImplementedException("GameManager.StartRound - Member 2");
        }

        public PickResult PlayCard(int handIndex)
        {
            // TODO (Member 2):
            // 1. card = Hand[handIndex]; result = card.Evaluate(CurrentDisaster)   <- polymorphism
            // 2. player.LoseLives(GetDamage(result, Level)); player.AddScore(GetPoints(result) * Level)
            // 3. bonus = player.UpdateStreak(result)
            // 4. Build LastMessage:
            //      MemeCard m  -> "WRONG! " + m.FunnyLine + "\nLesson: " + m.Lesson
            //      Perfect     -> "PERFECT! " + card.Name + " is the right move."
            //      Partial     -> "PARTIAL! " + card.Name + " helps, but it did not stop the " + disaster
            //      Wrong       -> "WRONG! " + card.Name + " is for a different disaster."
            //    Add "+1 life streak bonus!" if bonus is true.
            // 5. Remove the card from Hand. Discard it to the deck (but NOT if it is the Meme card).
            // 6. LastPlayed = card; LastResult = result
            // 7. If Lives == 0 -> IsGameOver = true
            //    Else if Level == MaxLevel and Round == RoundsPerLevel -> IsWin = true
            // 8. return result
            throw new NotImplementedException("GameManager.PlayCard - Member 2");
        }
    }
}
