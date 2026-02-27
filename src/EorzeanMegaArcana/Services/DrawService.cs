using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class DrawService
{
    public DrawResult Draw(SpreadDefinition spread, IReadOnlyList<Card> cards, bool allowRepeats, int? seed = null)
    {
        if (cards.Count < spread.CardCount && !allowRepeats)
        {
            throw new InvalidOperationException("Not enough cards to draw this spread without repeats.");
        }

        var random = new Random(seed ?? Environment.TickCount);
        var available = cards.ToList();
        var drawnCards = new List<DrawnCard>(spread.CardCount);

        foreach (var position in spread.Positions.OrderBy(item => item.Index))
        {
            var pool = allowRepeats ? cards : available;
            var card = pool[random.Next(pool.Count)];

            drawnCards.Add(new DrawnCard
            {
                Position = position,
                Card = card
            });

            if (!allowRepeats)
            {
                available.Remove(card);
            }
        }

        return new DrawResult
        {
            Spread = spread,
            DrawnCards = drawnCards,
            Seed = seed
        };
    }
}
