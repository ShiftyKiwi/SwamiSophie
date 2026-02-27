using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class ReadingSummaryTests
{
    private readonly InterpretationEngine engine = new();

    [Fact]
    public void SummaryIncludesTitleHeaderAndThreeCardLabels()
    {
        var result = Interpret(
            [
                Card("a", "Crystal", "Astral", 1),
                Card("b", "Persona", "Balanced", 0),
                Card("c", "Element", "Astral", 1, element: "Fire", rank: "1")
            ],
            axisIndex: 2,
            outputModeId: "scholarly",
            question: "What shape is this pattern taking?");

        var summary = ReadingTextFormatter.BuildSummaryText(result, Doctrine(), InterpretationBiasOptions.Auto, new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("Swami Sophie — Reading Summary", summary);
        Assert.Contains("Timestamp:", summary);
        Assert.Contains("Spread: test | Output: scholarly | Bias: Auto", summary);
        Assert.Contains("Question: What shape is this pattern taking?", summary);
        Assert.Contains("Scale:", summary);
        Assert.Contains("Pressure:", summary);
        Assert.Contains("Axis:", summary);
        Assert.Contains("Direction:", summary);
        Assert.DoesNotContain("Diagnostics", summary);
        Assert.DoesNotContain("Cosmic Authority", summary);
        Assert.Contains("— End —", summary);
    }

    [Fact]
    public void SummaryIncludesTitleHeaderAndNineCardLabels()
    {
        var result = Interpret(
            [
                Card("a1", "Crystal", "Astral", 1),
                Card("a2", "Persona", "Balanced", 0),
                Card("a3", "Element", "Astral", 1, element: "Fire", rank: "1"),
                Card("b1", "Primal", "Astral", 1),
                Card("b2", "Court", "Astral", 1, element: "Fire", rank: "Initiate"),
                Card("b3", "Element", "Balanced", 0, element: "Fire", rank: "2"),
                Card("c1", "Divine", "Balanced", 0),
                Card("c2", "Shard", "Umbral", -1),
                Card("c3", "Element", "Umbral", -1, element: "Ice", rank: "1")
            ],
            axisIndex: 5,
            spread: Spread("grid", 9, 5),
            outputModeId: "layered");

        var summary = ReadingTextFormatter.BuildSummaryText(result, Doctrine(), InterpretationBiasOptions.PreferCore, new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("Swami Sophie — Reading Summary", summary);
        Assert.Contains("Timestamp:", summary);
        Assert.Contains("Spread: grid | Output: layered | Bias: PreferCore", summary);
        Assert.Contains("Scale:", summary);
        Assert.Contains("First Row:", summary);
        Assert.Contains("Second Row:", summary);
        Assert.Contains("Third Row:", summary);
        Assert.DoesNotContain("Diagnostics", summary);
        Assert.DoesNotContain("Recommendation", summary);
        Assert.Contains("— End —", summary);
    }

    private ReadingResult Interpret(Card[] cards, int axisIndex, SpreadDefinition? spread = null, string outputModeId = "concise", string? question = null)
    {
        var actualSpread = spread ?? Spread("test", cards.Length, axisIndex);
        var draw = new DrawResult
        {
            Spread = actualSpread,
            DrawnCards = actualSpread.Positions.Select((position, index) => new DrawnCard
            {
                Position = position,
                Card = cards[index]
            }).ToArray()
        };

        var result = this.engine.Interpret(draw, Doctrine(), outputModeId, question);
        result.Narrative = ReadingFormatterFactory.Create(outputModeId, InterpretationBiasOptions.Auto).Format(result, Doctrine());
        return result;
    }

    private static SpreadDefinition Spread(string id, int count, int axisIndex)
    {
        return new SpreadDefinition
        {
            Id = id,
            Name = id,
            CardCount = count,
            Layout = count == 9 ? "grid3x3" : "row",
            AxisIndex = axisIndex,
            Positions = Enumerable.Range(1, count)
                .Select(index => new PositionDefinition
                {
                    Index = index,
                    Name = index == axisIndex ? "Axis" : $"Position {index}",
                    Description = $"Position {index}"
                })
                .ToArray()
        };
    }

    private static Card Card(string id, string stratum, string polarity, int polarityWeight, string? element = null, string? rank = null)
    {
        return new Card
        {
            Id = id,
            Name = id,
            Stratum = stratum,
            Element = element,
            Rank = rank,
            Polarity = polarity,
            PolarityWeight = polarityWeight,
            Core = $"{id} core",
            Shadow = $"{id} shadow",
            AstralNote = $"{id} astral note",
            UmbralNote = $"{id} umbral note",
            Flags = Array.Empty<string>()
        };
    }

    private static Doctrine Doctrine()
    {
        return new Doctrine
        {
            AuthorityOrder = new[] { "Calamity", "Crystal", "Divine", "Primal", "Shard", "Persona", "Court", "Element" },
            ScaleRules = new ScaleRules
            {
                Minor = new ScaleRuleSet
                {
                    Conditions = new[]
                    {
                        new ScaleCondition { StratumAbsent = "Calamity" },
                        new ScaleCondition { AxisNotStratum = "Crystal" },
                        new ScaleCondition { MaxCountByStratum = new Dictionary<string, int> { ["Primal"] = 1 } }
                    }
                },
                Major = new ScaleRuleSet
                {
                    ConditionsAny = new[]
                    {
                        new ScaleCondition { AxisIsStratum = "Crystal" },
                        new ScaleCondition { MinCountByStratum = new Dictionary<string, int> { ["Primal"] = 2 } }
                    }
                },
                Era = new ScaleRuleSet
                {
                    ConditionsAny = new[]
                    {
                        new ScaleCondition { AxisIsStratum = "Calamity" }
                    }
                }
            },
            Polarity = new PolarityRules
            {
                AstralThreshold = 2,
                UmbralThreshold = -2,
                DivineBalanceCardIds = new[] { "divine-radiant-balance" },
                StabilizeByOneTierIfBalancePresent = true
            }
        };
    }
}
