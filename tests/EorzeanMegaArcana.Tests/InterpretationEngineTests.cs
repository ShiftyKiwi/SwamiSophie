using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using EorzeanMegaArcana.Services.Formatters;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class InterpretationEngineTests
{
    private readonly InterpretationEngine engine = new();
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void AxisCalamityProducesEraScale()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Astral", 1), Card("b", "Calamity", "Umbral", -2), Card("c", "Element", "Astral", 1) },
            axisIndex: 2);

        Assert.Equal("Era", result.Header.Scale);
    }

    [Fact]
    public void AxisCrystalProducesMajorScale()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Astral", 1), Card("b", "Crystal", "Balanced", 0), Card("c", "Element", "Astral", 1) },
            axisIndex: 2);

        Assert.Equal("Major", result.Header.Scale);
    }

    [Fact]
    public void TwoPrimalsProduceMajorScale()
    {
        var result = Interpret(
            new[] { Card("a", "Primal", "Astral", 1), Card("b", "Element", "Balanced", 0), Card("c", "Primal", "Umbral", -1) },
            axisIndex: 2);

        Assert.Equal("Major", result.Header.Scale);
        Assert.True(result.Header.Escalation);
        Assert.Contains("Two or more Primal cards are present.", result.Header.EscalationReasons);
    }

    [Fact]
    public void NonCalamityNonCrystalAxisWithAtMostOnePrimalProducesMinorScale()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Astral", 1), Card("b", "Element", "Balanced", 0), Card("c", "Primal", "Umbral", -1) },
            axisIndex: 2);

        Assert.Equal("Minor", result.Header.Scale);
    }

    [Theory]
    [InlineData(2, "Astral")]
    [InlineData(-2, "Umbral")]
    [InlineData(0, "Transitional")]
    public void PolarityThresholdsMapToExpectedEraState(int polaritySum, string expected)
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Balanced", polaritySum) },
            axisIndex: 1);

        Assert.Equal(expected, result.Header.EraState);
    }

    [Fact]
    public void DivineBalanceCardMovesEraTowardTransitional()
    {
        var result = Interpret(
            new[] { Card("divine-radiant-balance", "Divine", "Astral", 2) },
            axisIndex: 1);

        Assert.Equal("Transitional", result.Header.EraState);
        Assert.True(result.Header.Moderation);
    }

    [Fact]
    public void CalamityPresenceTriggersEscalationAndAxisCalamityProducesEraScale()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Balanced", 0), Card("calamity-world-rewritten", "Calamity", "Umbral", -2), Card("b", "Persona", "Balanced", 0) },
            axisIndex: 2);

        Assert.True(result.Header.Escalation);
        Assert.Contains("A Calamity card is present.", result.Header.EscalationReasons);
        Assert.Equal("Era", result.Header.Scale);
    }

    [Fact]
    public void DivineBalanceStabilizesUmbralTowardTransitional()
    {
        var result = Interpret(
            new[]
            {
                Card("divine-radiant-balance", "Divine", "Balanced", 0),
                Card("b", "Element", "Umbral", -2)
            },
            axisIndex: 1,
            spread: Spread("row", 2, 1));

        Assert.Equal("Transitional", result.Header.EraState);
    }

    [Fact]
    public void DominantElementReturnsNullOnTie()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Element", "Astral", 1, element: "Fire", rank: "1"),
                Card("b", "Element", "Astral", 1, element: "Ice", rank: "1")
            },
            axisIndex: 1,
            spread: Spread("grid", 2, 1));

        Assert.Null(result.Header.DominantElement);
    }

    [Fact]
    public void DominantElementCountsCourtCardsAndResolvesFireMajority()
    {
        var result = Interpret(
            new[]
            {
                Card("court-fire-initiate", "Court", "Astral", 1, element: "Fire", rank: "Initiate"),
                Card("court-fire-vanguard", "Court", "Astral", 1, element: "Fire", rank: "Vanguard"),
                Card("fire-i", "Element", "Astral", 1, element: "Fire", rank: "1")
            },
            axisIndex: 2);

        Assert.Equal("Fire", result.Header.DominantElement);
    }

    [Fact]
    public void DominantElementReturnsNullWhenCourtAndElementCountsTie()
    {
        var result = Interpret(
            new[]
            {
                Card("court-fire-initiate", "Court", "Astral", 1, element: "Fire", rank: "Initiate"),
                Card("court-ice-initiate", "Court", "Umbral", -1, element: "Ice", rank: "Initiate"),
                Card("fire-i", "Element", "Astral", 1, element: "Fire", rank: "1"),
                Card("ice-i", "Element", "Umbral", -1, element: "Ice", rank: "1")
            },
            axisIndex: 2,
            spread: Spread("grid", 4, 2));

        Assert.Null(result.Header.DominantElement);
    }

    [Fact]
    public void UmbralEraWithAstralCardUsesShadow()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Astral", -3) },
            axisIndex: 1);

        Assert.True(result.DrawnCards[0].UsedShadow);
    }

    [Fact]
    public void AstralEraWithUmbralCardUsesShadow()
    {
        var result = Interpret(
            new[] { Card("a", "Element", "Umbral", 3) },
            axisIndex: 1);

        Assert.True(result.DrawnCards[0].UsedShadow);
    }

    [Fact]
    public void CalamityBiasesTowardShadowForNonDivineCards()
    {
        var result = Interpret(
            new[] { Card("a", "Calamity", "Balanced", 0), Card("b", "Element", "Balanced", 0) },
            axisIndex: 1,
            spread: Spread("grid", 2, 1));

        Assert.True(result.DrawnCards[1].UsedShadow);
    }

    [Fact]
    public void FormattersAvoidDeterministicLanguage()
    {
        var result = Interpret(
            new[] { Card("a", "Crystal", "Astral", 2), Card("b", "Element", "Astral", 1, element: "Fire", rank: "1"), Card("c", "Element", "Balanced", 0, element: "Fire", rank: "2") },
            axisIndex: 2);

        var doctrine = Doctrine();
        var narratives = new[]
        {
            new ConciseFormatter().Format(result, doctrine),
            new LayeredFormatter().Format(result, doctrine),
            new ScholarlyFormatter().Format(result, doctrine)
        };

        Assert.All(narratives, text => Assert.DoesNotContain("will happen", text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConciseFormatterUsesCoreNarrativeAndThreeCardHeadings()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Crystal", "Astral", 1),
                Card("b", "Persona", "Balanced", 0),
                Card("c", "Element", "Astral", 1, element: "Fire", rank: "1")
            },
            axisIndex: 2);

        var text = new ConciseFormatter().Format(result, Doctrine());

        Assert.Contains("Core Narrative", text);
        Assert.Contains("Pressure", text);
        Assert.Contains("Axis", text);
        Assert.Contains("Direction", text);
    }

    [Fact]
    public void ConciseFormatterUsesRowSummariesForNineCardSpread()
    {
        var result = Interpret(
            new[]
            {
                Card("a1", "Crystal", "Astral", 1),
                Card("a2", "Persona", "Balanced", 0),
                Card("a3", "Element", "Astral", 1, element: "Fire", rank: "1"),
                Card("b1", "Primal", "Astral", 1),
                Card("b2", "Court", "Astral", 1, element: "Fire", rank: "Initiate"),
                Card("b3", "Element", "Balanced", 0, element: "Fire", rank: "2"),
                Card("c1", "Divine", "Balanced", 0),
                Card("c2", "Shard", "Umbral", -1),
                Card("c3", "Element", "Umbral", -1, element: "Ice", rank: "1")
            },
            axisIndex: 5,
            spread: Spread("grid", 9, 5));

        var text = new ConciseFormatter().Format(result, Doctrine());

        Assert.Contains("Core Narrative", text);
        Assert.Contains("First Row", text);
        Assert.Contains("Second Row", text);
        Assert.Contains("Third Row", text);
    }

    [Fact]
    public void LayeredFormatterUsesExactSpecHeaders()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Crystal", "Astral", 1),
                Card("persona-the-witness", "Persona", "Balanced", 0),
                Card("shard-fractured-oath", "Shard", "Umbral", -1)
            },
            axisIndex: 2);

        var text = new LayeredFormatter().Format(result, Doctrine());

        Assert.Contains("Cosmic Authority", text);
        Assert.Contains("Amplification & Distortion", text);
        Assert.Contains("Personal Lens", text);
        Assert.Contains("Manifestation", text);
        Assert.Contains("Recommendation", text);
    }

    [Fact]
    public void ScholarlyFormatterLabelsDiagnosticsClearly()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Crystal", "Astral", 2),
                Card("b", "Element", "Astral", 1, element: "Fire", rank: "1"),
                Card("c", "Element", "Balanced", 0, element: "Fire", rank: "2")
            },
            axisIndex: 2);

        var text = new ScholarlyFormatter().Format(result, Doctrine());

        Assert.Contains("Diagnostics", text);
        Assert.Contains("Polarity Sum and Thresholds", text);
        Assert.Contains("Counts By Stratum", text);
        Assert.Contains("Counts By Element", text);
    }

    [Fact]
    public void DeterministicCoverageAcrossRealDeckHitsAllScalesAndProducesNarratives()
    {
        var service = CreateReadingService();

        foreach (var spreadId in new[] { "aether-pulse", "convergence-of-the-star" })
        {
            var seenScales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var seed = 1; seed <= 500; seed++)
            {
                var draw = service.Draw(spreadId, allowRepeats: false, seed: seed);
                var result = service.Interpret(draw, "concise");

                Assert.False(string.IsNullOrWhiteSpace(result.Narrative));
                seenScales.Add(result.Header.Scale);
            }

            Assert.Contains("Minor", seenScales);
            Assert.Contains("Major", seenScales);
            Assert.Contains("Era", seenScales);
        }
    }

    [Fact]
    public void DeterministicCoverageAcrossConvergenceProducesDominantElementMajority()
    {
        var service = CreateReadingService();
        var nonNullDominantElementCount = 0;

        for (var seed = 1; seed <= 500; seed++)
        {
            var draw = service.Draw("convergence-of-the-star", allowRepeats: false, seed: seed);
            var result = service.Interpret(draw, "concise");

            Assert.False(string.IsNullOrWhiteSpace(result.Narrative));
            if (!string.IsNullOrWhiteSpace(result.Header.DominantElement))
            {
                nonNullDominantElementCount++;
            }
        }

        Assert.True(
            nonNullDominantElementCount > 250,
            $"Expected a majority of Convergence draws to produce a dominant element, but only {nonNullDominantElementCount} of 500 did.");
    }

    [Fact]
    public void ShardPresenceAppearsInLayeredAndScholarlyBreakdown()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Crystal", "Astral", 1),
                Card("shard-fractured-oath", "Shard", "Umbral", -1, flags: new[] { "Distort" }),
                Card("b", "Element", "Balanced", 0, element: "Fire", rank: "1")
            },
            axisIndex: 2);

        var doctrine = Doctrine();
        var layered = new LayeredFormatter().Format(result, doctrine);
        var scholarly = new ScholarlyFormatter().Format(result, doctrine);

        Assert.Contains("Amplification & Distortion", layered);
        Assert.Contains("shard-fractured-oath", layered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Amplification & Distortion", scholarly);
        Assert.Contains("shard-fractured-oath", scholarly, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersonaAppearsUnderPersonalLensInLayeredBreakdown()
    {
        var result = Interpret(
            new[]
            {
                Card("a", "Crystal", "Astral", 1),
                Card("persona-the-witness", "Persona", "Balanced", 0),
                Card("b", "Element", "Balanced", 0, element: "Fire", rank: "1")
            },
            axisIndex: 2);

        var layered = new LayeredFormatter().Format(result, Doctrine());

        Assert.Contains("Personal Lens", layered);
        Assert.Contains("persona-the-witness", layered, StringComparison.OrdinalIgnoreCase);
    }

    private ReadingResult Interpret(Card[] cards, int axisIndex, SpreadDefinition? spread = null)
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

        var result = this.engine.Interpret(draw, Doctrine(), "concise");
        result.Narrative = ReadingFormatterFactory.Create("concise").Format(result, Doctrine());
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

    private static Card Card(string id, string stratum, string polarity, int polarityWeight, string? element = null, string? rank = null, IReadOnlyList<string>? flags = null)
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
            Flags = flags ?? Array.Empty<string>()
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

    private static ReadingService CreateReadingService()
    {
        var dataRoot = Path.Combine(RepoRoot, "data");
        var dataBundle = new DataLoader(dataRoot).Load();
        return new ReadingService(
            dataBundle,
            new DeckService(dataBundle.AllCards),
            new DrawService(),
            new InterpretationEngine());
    }
}
