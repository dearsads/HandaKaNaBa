// INTEGRATION CONTRACT (doc section 10). Do not change without approval from all 4 members.
// The UI only talks to this interface, so FakeGameManager and GameManager can be swapped
// by changing ONE line in MenuForm.CreateGame().
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public interface IGameManager
    {
        int Level { get; }
        int Round { get; }
        int RoundsPerLevel { get; }
        int Lives { get; }
        int MaxLives { get; }
        int Score { get; }
        bool IsGameOver { get; }
        bool IsWin { get; }

        DisasterCard CurrentDisaster { get; }
        List<ResponseCard> Hand { get; }

        ResponseCard LastPlayed { get; }   // null before the first pick
        PickResult LastResult { get; }
        string LastMessage { get; }        // result text; for a MemeCard: funny line + lesson

        void StartGame();
        void StartRound();
        PickResult PlayCard(int handIndex); // evaluates, updates lives and score, refills hand
    }
}
