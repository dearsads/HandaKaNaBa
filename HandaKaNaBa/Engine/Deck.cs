// Owner: Back End (Members 1 & 2)
// A draw pile and a discard pile. When the draw pile runs out, the discard pile
// is shuffled back in, so Draw() never runs out during a 25-round game.
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
        public int DiscardCount => discardPile.Count;

        /// <summary>Randomly reorder the draw pile (Fisher-Yates shuffle).</summary>
        public void Shuffle()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = drawPile[i];
                drawPile[i] = drawPile[j];
                drawPile[j] = temp;
            }
        }

        /// <summary>Take the top card. If the draw pile is empty, shuffle the discard pile back in.</summary>
        public T Draw()
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                    throw new InvalidOperationException("The deck and the discard pile are both empty.");

                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle();
            }

            T top = drawPile[0];
            drawPile.RemoveAt(0);
            return top;
        }

        public void Discard(T card)
        {
            discardPile.Add(card);
        }

        /// <summary>
        /// Used by the fairness rule: find a card that matches, remove it from its pile and return it.
        /// Searches the draw pile first, then the discard pile. Returns null if none is found.
        /// </summary>
        public T TakeFirst(Predicate<T> match)
        {
            int index = drawPile.FindIndex(match);
            if (index >= 0)
            {
                T found = drawPile[index];
                drawPile.RemoveAt(index);
                return found;
            }

            index = discardPile.FindIndex(match);
            if (index >= 0)
            {
                T found = discardPile[index];
                discardPile.RemoveAt(index);
                return found;
            }

            return null;
        }
    }
}
