// Owner: Member 1
namespace HandaKaNaBa.Models
{
    /// <summary>
    /// A funny but unsafe choice. Always Wrong, but it carries a joke and the real lesson.
    /// Meme cards are NOT in the main deck: GameManager adds one per round (see Meme rule).
    /// </summary>
    public class MemeCard : ResponseCard
    {
        public DisasterType ForDisaster { get; }
        public string FunnyLine { get; }
        public string Lesson { get; }

        public MemeCard(string name, string emoji, DisasterType forDisaster, string funnyLine, string lesson)
            : base(name, emoji, "???")
        {
            ForDisaster = forDisaster;
            FunnyLine = funnyLine;
            Lesson = lesson;
        }

        public override PickResult Evaluate(DisasterCard disaster)
        {
            return PickResult.Wrong;
        }
    }
}
