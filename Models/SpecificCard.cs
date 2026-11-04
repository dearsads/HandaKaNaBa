// Owner: Member 1
namespace HandaKaNaBa.Models
{
    /// <summary>A card made for one disaster: Perfect for that disaster, Wrong for the rest.</summary>
    public class SpecificCard : ResponseCard
    {
        public DisasterType PrimaryDisaster { get; }

        public SpecificCard(string name, string emoji, string description, DisasterType primaryDisaster)
            : base(name, emoji, description)
        {
            PrimaryDisaster = primaryDisaster;
        }

        public override PickResult Evaluate(DisasterCard disaster)
        {
            return disaster.Type == PrimaryDisaster ? PickResult.Perfect : PickResult.Wrong;
        }
    }
}
