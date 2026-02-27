using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.UI.Windows;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class DeckBrowserWindowTests
{
    [Fact]
    public void SearchAndFiltersReturnExpectedCards()
    {
        var cards = new[]
        {
            Card("court-fire-initiate", "Fire Initiate", "Court", "Fire", "Initiate", []),
            Card("shard-fractured-oath", "Shard of the Fractured Oath", "Shard", null, null, ["Distort"]),
            Card("divine-radiant-balance", "Radiant Balance", "Divine", null, null, ["Moderate"])
        };

        var bySearch = DeckBrowserSearch.Filter(cards, "fractured", "All", "All", []);
        Assert.Single(bySearch);
        Assert.Equal("shard-fractured-oath", bySearch[0].Id);

        var byElement = DeckBrowserSearch.Filter(cards, null, "Court", "Fire", []);
        Assert.Single(byElement);
        Assert.Equal("court-fire-initiate", byElement[0].Id);

        var byFlag = DeckBrowserSearch.Filter(cards, null, "All", "All", ["Moderate"]);
        Assert.Single(byFlag);
        Assert.Equal("divine-radiant-balance", byFlag[0].Id);
    }

    [Fact]
    public void StratumLabelsIncludeCounts()
    {
        var cards = new[]
        {
            Card("crystal-i", "Crystal I", "Crystal", null, null, []),
            Card("crystal-ii", "Crystal II", "Crystal", null, null, []),
            Card("court-fire-initiate", "Fire Initiate", "Court", "Fire", "Initiate", [])
        };

        var labels = DeckBrowserSearch.BuildStratumLabels(cards, ["All", "Court", "Crystal"]);

        Assert.Equal("All (3)", labels[0]);
        Assert.Equal("Court (1)", labels[1]);
        Assert.Equal("Crystal (2)", labels[2]);
    }

    [Fact]
    public void RandomCardSelectsAVisibleCardWhenResultsExist()
    {
        var cards = new[]
        {
            Card("court-fire-initiate", "Fire Initiate", "Court", "Fire", "Initiate", []),
            Card("shard-fractured-oath", "Shard of the Fractured Oath", "Shard", null, null, ["Distort"])
        };

        var visible = DeckBrowserSearch.Filter(cards, null, "All", "All", []);
        var randomCard = DeckBrowserSearch.SelectRandom(visible, new Random(7));

        Assert.NotNull(randomCard);
        Assert.Contains(randomCard!, visible);
    }

    private static Card Card(string id, string name, string stratum, string? element, string? rank, IReadOnlyList<string> flags)
    {
        return new Card
        {
            Id = id,
            Name = name,
            Stratum = stratum,
            Element = element,
            Rank = rank,
            Polarity = "Balanced",
            PolarityWeight = 0,
            Core = $"{id} core",
            Shadow = $"{id} shadow",
            AstralNote = $"{id} astral",
            UmbralNote = $"{id} umbral",
            Flags = flags
        };
    }
}
