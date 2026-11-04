// Owner: Back End (Members 1 & 2)
namespace HandaKaNaBa.Models
{
    /// <summary>Useful in any disaster, but never enough on its own: always Partial.</summary>
    public class GeneralCard : ResponseCard
    {
        public GeneralCard(string name, string emoji, string description)
            : base(name, emoji, description)
        {
        }

        public override PickResult Evaluate(DisasterCard disaster)
        {
            return PickResult.Partial;
        }
    }
}
