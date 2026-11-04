// Owner: Member 2
// Done when: Draw() never crashes, even after all 30 cards have been drawn (reshuffle the discard pile).
using HandaKaNaBa.Models;

namespace HandaKaNaBa.Engine
{
    public class Deck<T> where T : Card
    {
        private readonly List<T> drawPile = new List<T>();
        private readonly List<T> discardPile = new List<T>();
        private readonly Random random;

        public Deck(IEnumerable<T> cards, Random random)
        {
            drawPile.AddRange(cards);
            this.random = random;
        }

        public int Count => drawPile.Count;

        /// <summary>Randomly reorder the draw pile (Fisher-Yates shuffle).</summary>
        public void Shuffle()
        {
            // TODO (Member 2):
            // for i from last index down to 1:
            //     j = random.Next(i + 1)
            //     swap drawPile[i] and drawPile[j]
            throw new NotImplementedException("Deck.Shuffle - Member 2");
        }

        /// <summary>Take the top card. If the draw pile is empty, move the discard pile back and shuffle.</summary>
        public T Draw()
        {
            // TODO (Member 2):
            // 1. if drawPile is empty: add all discardPile cards to drawPile, clear discardPile, Shuffle()
            // 2. take drawPile[0], remove it, return it
            throw new NotImplementedException("Deck.Draw - Member 2");
        }

        public void Discard(T card)
        {
            // TODO (Member 2): add the card to discardPile
            throw new NotImplementedException("Deck.Discard - Member 2");
        }

        /// <summary>Used by the fairness rule: find and remove a card that matches a condition.</summary>
        public T TakeFirst(Predicate<T> match)
        {
            // TODO (Member 2): search drawPile (then discardPile) for a card where match(card) is true,
            // remove it from that pile and return it. Return null if none is found.
            throw new NotImplementedException("Deck.TakeFirst - Member 2");
        }
    }
}
