// Owner: Member 1
// ABSTRACTION: Card cannot be created directly. Every card in the game is a subclass.
namespace HandaKaNaBa.Models
{
    public abstract class Card
    {
        public string Name { get; }
        public string Emoji { get; }
        public string Description { get; }

        protected Card(string name, string emoji, string description)
        {
            Name = name;
            Emoji = emoji;
            Description = description;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
