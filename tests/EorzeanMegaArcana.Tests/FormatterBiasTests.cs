using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using EorzeanMegaArcana.Services.Formatters;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class FormatterBiasTests
{
    private readonly InterpretationEngine engine = new();

    [Fact]
    public void PreferShadowChoosesShadowWhenAutoWouldChooseCore()
    {
        var result = Interpret(
            [
                Card("steady-axis", "Element", "Balanced", 0, core: "core meaning", shadow: "shadow meaning", element: "Fire", rank: "1")
            ],
            axisIndex: 1,
            eraStateWeightCardCount: 1);

        var autoText = new ConciseFormatter(InterpretationBiasOptions.Auto).Format(result, Doctrine());
        var preferShadowText = new ConciseFormatter(InterpretationBiasOptions.PreferShadow).Format(result, Doctrine());

        Assert.Contains("core meaning", autoText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shadow meaning", autoText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shadow meaning", preferShadowText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreferCoreChoosesCoreWhenAutoWouldChooseShadowAndFallsBackSafely()
    {
        var mismatchResult = Interpret(
            [
                Card("umbral-card", "Element", "Umbral", -2, core: "core meaning", shadow: "shadow meaning", element: "Ice", rank: "1")
            ],
            axisIndex: 1,
            eraStateWeightCardCount: 1,
            forceAstralEra: true);

        var autoText = new ConciseFormatter(InterpretationBiasOptions.Auto).Format(mismatchResult, Doctrine());
        var preferCoreText = new ConciseFormatter(InterpretationBiasOptions.PreferCore).Format(mismatchResult, Doctrine());

        Assert.Contains("shadow meaning", autoText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("core meaning", preferCoreText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shadow meaning", preferCoreText, StringComparison.OrdinalIgnoreCase);

        var fallbackResult = Interpret(
            [
                Card("umbral-empty-core", "Element", "Umbral", -2, core: string.Empty, shadow: "fallback shadow", element: "Ice", rank: "1")
            ],
            axisIndex: 1,
            eraStateWeightCardCount: 1,
            forceAstralEra: true);

        var fallbackText = new ConciseFormatter(InterpretationBiasOptions.PreferCore).Format(fallbackResult, Doctrine());
        Assert.Contains("fallback shadow", fallbackText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StrictAutoDiffersFromAutoOnlyInCalamityBiasBehavior()
    {
        var calamityBiasResult = Interpret(
            [
                Card("calamity-world-rewritten", "Calamity", "Balanced", 0, core: "calamity core", shadow: "calamity shadow"),
                Card("balanced-element", "Element", "Balanced", 0, core: "core meaning", shadow: "shadow meaning", element: "Fire", rank: "1")
            ],
            axisIndex: 1,
            eraStateWeightCardCount: 2);

        var autoText = new ConciseFormatter(InterpretationBiasOptions.Auto).Format(calamityBiasResult, Doctrine());
        var strictAutoText = new ConciseFormatter(InterpretationBiasOptions.StrictAuto).Format(calamityBiasResult, Doctrine());

        Assert.Contains("shadow meaning", autoText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("core meaning", strictAutoText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shadow meaning", strictAutoText, StringComparison.OrdinalIgnoreCase);

        var mismatchResult = Interpret(
            [
                Card("umbral-card", "Element", "Umbral", -2, core: "core meaning", shadow: "shadow meaning", element: "Ice", rank: "1")
            ],
            axisIndex: 1,
            eraStateWeightCardCount: 1,
            forceAstralEra: true);

        var autoMismatchText = new ConciseFormatter(InterpretationBiasOptions.Auto).Format(mismatchResult, Doctrine());
        var strictAutoMismatchText = new ConciseFormatter(InterpretationBiasOptions.StrictAuto).Format(mismatchResult, Doctrine());

        Assert.Contains("shadow meaning", autoMismatchText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shadow meaning", strictAutoMismatchText, StringComparison.OrdinalIgnoreCase);
    }

    private ReadingResult Interpret(Card[] cards, int axisIndex, int eraStateWeightCardCount, bool forceAstralEra = false)
    {
        var spread = Spread(cards.Length, axisIndex);
        var adjustedCards = cards.ToArray();

        if (forceAstralEra)
        {
            adjustedCards[0].PolarityWeight = Math.Abs(adjustedCards[0].PolarityWeight);
            adjustedCards[0].Polarity = "Umbral";
            if (adjustedCards[0].PolarityWeight < 2)
            {
                adjustedCards[0].PolarityWeight = 2;
            }
        }

        var draw = new DrawResult
        {
            Spread = spread,
            DrawnCards = spread.Positions.Select((position, index) => new DrawnCard
            {
                Position = position,
                Card = adjustedCards[index]
            }).ToArray()
        };

        var result = this.engine.Interpret(draw, Doctrine(), "concise");
        return result;
    }

    private static SpreadDefinition Spread(int count, int axisIndex)
    {
        return new SpreadDefinition
        {
            Id = "test",
            Name = "test",
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

    private static Card Card(string id, string stratum, string polarity, int polarityWeight, string core, string shadow, string? element = null, string? rank = null)
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
            Core = core,
            Shadow = shadow,
            AstralNote = "astral note",
            UmbralNote = "umbral note",
            Flags = Array.Empty<string>()
        };
    }

    private static Doctrine Doctrine()
    {
        return new Doctrine
        {
            AuthorityOrder = ["Calamity", "Crystal", "Divine", "Primal", "Shard", "Persona", "Court", "Element"],
            ScaleRules = new ScaleRules
            {
                Minor = new ScaleRuleSet
                {
                    Conditions =
                    [
                        new ScaleCondition { StratumAbsent = "Calamity" },
                        new ScaleCondition { AxisNotStratum = "Crystal" },
                        new ScaleCondition { MaxCountByStratum = new Dictionary<string, int> { ["Primal"] = 1 } }
                    ]
                },
                Major = new ScaleRuleSet
                {
                    ConditionsAny =
                    [
                        new ScaleCondition { AxisIsStratum = "Crystal" },
                        new ScaleCondition { MinCountByStratum = new Dictionary<string, int> { ["Primal"] = 2 } }
                    ]
                },
                Era = new ScaleRuleSet
                {
                    ConditionsAny =
                    [
                        new ScaleCondition { AxisIsStratum = "Calamity" }
                    ]
                }
            },
            Polarity = new PolarityRules
            {
                AstralThreshold = 2,
                UmbralThreshold = -2,
                DivineBalanceCardIds = ["divine-radiant-balance"],
                StabilizeByOneTierIfBalancePresent = true
            }
        };
    }
}
