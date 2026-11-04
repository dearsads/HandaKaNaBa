// Owner: Back End (Members 1 & 2)
// INHERITANCE: DisasterCard is a Card.
namespace HandaKaNaBa.Models
{
    public class DisasterCard : Card
    {
        public DisasterType Type { get; }
        public int Severity { get; }

        /// <summary>The scenario line shown under the comic panels.</summary>
        public string Scenario { get; }

        public DisasterCard(DisasterType type, string emoji, string scenario, int severity = 1)
            : base(type.ToString(), emoji, scenario)
        {
            Type = type;
            Scenario = scenario;
            Severity = severity;
        }
    }
}
