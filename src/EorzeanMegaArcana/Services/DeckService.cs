using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class DeckService
{
    private readonly IReadOnlyList<Card> cards;
    private readonly IReadOnlyDictionary<string, Card> cardById;

    public DeckService(IReadOnlyList<Card> cards)
    {
        this.cards = cards.OrderBy(card => card.Stratum).ThenBy(card => card.Name).ToArray();
        this.cardById = this.cards.ToDictionary(card => card.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Card> GetAllCards() => this.cards;

    public IReadOnlyList<Card> GetByStratum(string stratum) =>
        this.cards.Where(card => string.Equals(card.Stratum, stratum, StringComparison.OrdinalIgnoreCase)).ToArray();

    public IReadOnlyList<Card> GetByElement(string element) =>
        this.cards.Where(card => string.Equals(card.Element, element, StringComparison.OrdinalIgnoreCase)).ToArray();

    public Card GetById(string id)
    {
        return this.cardById.TryGetValue(id, out var card)
            ? card
            : throw new KeyNotFoundException($"Unknown card id: {id}");
    }

    public IReadOnlyDictionary<string, int> GetCountsByStratum() =>
        this.cards.GroupBy(card => card.Stratum, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> GetCountsByElement() =>
        this.cards.Where(card => !string.IsNullOrWhiteSpace(card.Element))
            .GroupBy(card => card.Element!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
}
