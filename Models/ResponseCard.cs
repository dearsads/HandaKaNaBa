// Owner: Member 1
// POLYMORPHISM: each subclass overrides Evaluate() in its own way.
namespace HandaKaNaBa.Models
{
    public abstract class ResponseCard : Card
    {
        protected ResponseCard(string name, string emoji, string description)
            : base(name, emoji, description)
        {
        }

        /// <summary>How well this card answers the disaster.</summary>
        public virtual PickResult Evaluate(DisasterCard disaster)
        {
            return PickResult.Wrong;
        }
    }
}
