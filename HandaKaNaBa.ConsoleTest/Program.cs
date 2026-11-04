// Console test for the back end (no forms needed).
// Done when: every check prints PASS and the summary says "ALL TESTS PASSED".
// Run it in Visual Studio: right-click HandaKaNaBa.ConsoleTest > Set as Startup Project > F5.
using System.Text;
using HandaKaNaBa.Data;
using HandaKaNaBa.Engine;
using HandaKaNaBa.Models;

namespace HandaKaNaBa.ConsoleTest
{
    internal static class Program
    {
        private static int passed;
        private static int failed;

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Handa Ka Na Ba? back end test ===");
            Console.WriteLine();

            TestCardData();
            TestEvaluate();
            TestDeck();
            TestPlayer();
            TestRulesTables();
            TestPerfectPlayerWins();
            TestMemePlayerLoses();
            TestRandomGames(1000);

            Console.WriteLine();
            PrintSampleGame();

            Console.WriteLine();
            Console.WriteLine("Passed: " + passed + "   Failed: " + failed);
            Console.WriteLine(failed == 0 ? "ALL TESTS PASSED" : "SOME TESTS FAILED - see the FAIL lines above");
            Console.WriteLine("Press Enter to close.");
            Console.ReadLine();
        }

        private static void Check(bool condition, string name)
        {
            if (condition) passed++; else failed++;
            Console.WriteLine((condition ? "PASS  " : "FAIL  ") + name);
        }

        // ---------------------------------------------------------------

        private static void TestCardData()
        {
            Console.WriteLine("[Card data]");
            Check(CardData.Disasters().Count == 6, "6 disaster cards");
            Check(CardData.ResponseCards().Count == 30, "30 response cards (24 specific + 6 general)");
            Check(CardData.ResponseCards().OfType<GeneralCard>().Count() == 6, "6 general cards");
            Check(CardData.MemeCards().Count == 12, "12 meme cards");
            foreach (DisasterType type in Enum.GetValues(typeof(DisasterType)))
                Check(CardData.MemeCards().Count(m => m.ForDisaster == type) == 2, "2 meme cards for " + type);
        }

        private static void TestEvaluate()
        {
            Console.WriteLine("[Evaluate - every card against every disaster]");
            bool allCorrect = true;
            foreach (DisasterCard disaster in CardData.Disasters())
            {
                int perfect = 0, partial = 0, wrong = 0;
                foreach (ResponseCard card in CardData.ResponseCards())
                {
                    PickResult expected;
                    if (card is SpecificCard s)
                        expected = s.PrimaryDisaster == disaster.Type ? PickResult.Perfect : PickResult.Wrong;
                    else
                        expected = PickResult.Partial;

                    PickResult actual = card.Evaluate(disaster);
                    if (actual != expected)
                    {
                        allCorrect = false;
                        Console.WriteLine("      " + card.Name + " vs " + disaster.Name + ": got " + actual + ", expected " + expected);
                    }

                    if (actual == PickResult.Perfect) perfect++;
                    else if (actual == PickResult.Partial) partial++;
                    else wrong++;
                }
                Check(perfect == 4 && partial == 6 && wrong == 20,
                      disaster.Name + ": 4 Perfect, 6 Partial, 20 Wrong (got " + perfect + "/" + partial + "/" + wrong + ")");
            }
            Check(allCorrect, "every response card returns the expected result");

            bool memesWrong = CardData.MemeCards().All(m => CardData.Disasters().All(d => m.Evaluate(d) == PickResult.Wrong));
            Check(memesWrong, "meme cards are always Wrong");
        }

        private static void TestDeck()
        {
            Console.WriteLine("[Deck]");
            var deck = new Deck<ResponseCard>(CardData.ResponseCards(), new Random(1));
            deck.Shuffle();
            Check(deck.Count == 30, "deck starts with 30 cards");

            bool noCrash = true;
            try
            {
                for (int i = 0; i < 100; i++)
                    deck.Discard(deck.Draw());   // draws 100 cards from a 30-card deck
            }
            catch (Exception ex)
            {
                noCrash = false;
                Console.WriteLine("      " + ex.Message);
            }
            Check(noCrash, "100 draws reshuffle the discard pile without crashing");
            Check(deck.Count + deck.DiscardCount == 30, "no cards lost or duplicated after reshuffling");

            ResponseCard fire = deck.TakeFirst(c => c is SpecificCard s && s.PrimaryDisaster == DisasterType.Fire);
            Check(fire != null && deck.Count + deck.DiscardCount == 29, "TakeFirst finds and removes a Fire card");
        }

        private static void TestPlayer()
        {
            Console.WriteLine("[Player]");
            var p = new Player();
            Check(p.Lives == 5 && p.Score == 0, "starts with 5 lives and 0 score");
            p.LoseLives(2);
            Check(p.Lives == 3, "LoseLives(2) -> 3 lives");
            p.LoseLives(10);
            Check(p.Lives == 0, "lives never go below 0");

            p.Reset();
            p.LoseLives(1);
            p.UpdateStreak(PickResult.Perfect);
            p.UpdateStreak(PickResult.Perfect);
            bool bonus = p.UpdateStreak(PickResult.Perfect);
            Check(bonus && p.Lives == 5, "3 Perfects in a row gives +1 life");

            p.UpdateStreak(PickResult.Perfect);
            p.UpdateStreak(PickResult.Perfect);
            bonus = p.UpdateStreak(PickResult.Perfect);
            Check(!bonus && p.Lives == 5, "bonus life never goes above the maximum");

            p.UpdateStreak(PickResult.Perfect);
            p.UpdateStreak(PickResult.Wrong);
            Check(p.Streak == 0, "a non-Perfect pick resets the streak");
        }

        private static void TestRulesTables()
        {
            Console.WriteLine("[Rules tables]");
            Check(GameManager.GetHandSize(1) == 5 && GameManager.GetHandSize(2) == 5, "Levels 1-2: 5 cards");
            Check(GameManager.GetHandSize(3) == 4 && GameManager.GetHandSize(4) == 4, "Levels 3-4: 4 cards");
            Check(GameManager.GetHandSize(5) == 3, "Level 5: 3 cards");
            Check(GameManager.GetDamage(PickResult.Partial, 1) == 1 && GameManager.GetDamage(PickResult.Wrong, 1) == 2, "Level 1 damage: -1 / -2");
            Check(GameManager.GetDamage(PickResult.Partial, 3) == 1 && GameManager.GetDamage(PickResult.Wrong, 3) == 3, "Level 3 damage: -1 / -3");
            Check(GameManager.GetDamage(PickResult.Partial, 5) == 2 && GameManager.GetDamage(PickResult.Wrong, 5) == 3, "Level 5 damage: -2 / -3");
        }

        private static void TestPerfectPlayerWins()
        {
            Console.WriteLine("[A player who always picks a Perfect card]");
            var game = new GameManager(new Random(7));
            game.StartGame();
            int rounds = 0;
            while (!game.IsGameOver && !game.IsWin && rounds < 100)
            {
                game.StartRound();
                int index = game.Hand.FindIndex(c => c.Evaluate(game.CurrentDisaster) == PickResult.Perfect);
                game.PlayCard(index);
                rounds++;
            }
            Check(game.IsWin && !game.IsGameOver, "wins the game (Community Saved!)");
            Check(rounds == 25, "the game lasts exactly 25 rounds (got " + rounds + ")");
            Check(game.Lives == 5, "still has 5 lives");
            Check(game.Score == 750, "score is 750 = 10 x (1+2+3+4+5) x 5 rounds (got " + game.Score + ")");
        }

        private static void TestMemePlayerLoses()
        {
            Console.WriteLine("[A player who always picks the Meme card]");
            var game = new GameManager(new Random(3));
            game.StartGame();
            int rounds = 0;
            while (!game.IsGameOver && !game.IsWin && rounds < 100)
            {
                game.StartRound();
                game.PlayCard(game.Hand.FindIndex(c => c is MemeCard));
                rounds++;
            }
            Check(game.IsGameOver && !game.IsWin, "loses the game");
            Check(rounds == 3, "Game Over in round 3 (5 lives, -2 each) (got " + rounds + ")");
            Check(game.Score == 0, "score stays 0");
        }

        private static void TestRandomGames(int games)
        {
            Console.WriteLine("[" + games + " games with random picks]");
            var random = new Random(2026);
            bool handSizeOk = true, oneMemeOk = true, fairOk = true, noRepeatOk = true, endsOk = true;
            int wins = 0, losses = 0;

            for (int g = 0; g < games; g++)
            {
                var game = new GameManager(new Random(g));
                game.StartGame();
                DisasterType? previous = null;
                int rounds = 0;

                while (!game.IsGameOver && !game.IsWin && rounds < 30)
                {
                    game.StartRound();
                    rounds++;

                    if (game.Hand.Count != GameManager.GetHandSize(game.Level)) handSizeOk = false;
                    if (game.Hand.Count(c => c is MemeCard) != 1) oneMemeOk = false;
                    if (!game.Hand.Any(c => c.Evaluate(game.CurrentDisaster) == PickResult.Perfect)) fairOk = false;
                    if (previous == game.CurrentDisaster.Type) noRepeatOk = false;
                    previous = game.CurrentDisaster.Type;

                    game.PlayCard(random.Next(game.Hand.Count));
                }

                if (game.IsWin == game.IsGameOver || rounds > 25) endsOk = false;
                if (game.IsWin) wins++; else losses++;
            }

            Check(handSizeOk, "hand size always matches the level (5 / 4 / 3)");
            Check(oneMemeOk, "every hand has exactly one Meme card");
            Check(fairOk, "fairness rule: every hand has at least one Perfect card");
            Check(noRepeatOk, "the same disaster never appears twice in a row");
            Check(endsOk, "every game ends in either a win or a Game Over within 25 rounds");
            Console.WriteLine("      random-pick results: " + wins + " wins, " + losses + " losses");
        }

        private static void PrintSampleGame()
        {
            Console.WriteLine("=== Sample game (first 5 rounds, random picks) ===");
            var game = new GameManager(new Random(11));
            var picker = new Random(5);
            game.StartGame();

            for (int i = 0; i < 5 && !game.IsGameOver && !game.IsWin; i++)
            {
                game.StartRound();
                Console.WriteLine();
                Console.WriteLine("LEVEL " + game.Level + " | Round " + game.Round + "/" + game.RoundsPerLevel
                                  + " | Lives: " + game.Lives + " | Score: " + game.Score);
                Console.WriteLine("DISASTER: " + game.CurrentDisaster.Name.ToUpper() + " - " + game.CurrentDisaster.Scenario);
                Console.WriteLine("Your cards:");
                for (int c = 0; c < game.Hand.Count; c++)
                    Console.WriteLine("  [" + (c + 1) + "] " + game.Hand[c].Name);

                int pick = picker.Next(game.Hand.Count);
                Console.WriteLine("Pick: " + (pick + 1));
                game.PlayCard(pick);
                Console.WriteLine("-> " + game.LastMessage.Replace(Environment.NewLine, Environment.NewLine + "   "));
            }
        }
    }
}
