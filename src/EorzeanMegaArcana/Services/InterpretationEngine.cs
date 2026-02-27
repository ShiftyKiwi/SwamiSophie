using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class InterpretationEngine
{
    public ReadingResult Interpret(DrawResult drawResult, Doctrine doctrine, string outputModeId, string? question = null)
    {
        var countsByStratum = drawResult.DrawnCards
            .GroupBy(item => item.Card.Stratum, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var countsByElement = drawResult.DrawnCards
            .Where(item => !string.IsNullOrWhiteSpace(item.Card.Element))
            .GroupBy(item => item.Card.Element!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var matchedScaleConditions = new List<string>();
        var scale = DetermineScale(drawResult, doctrine, countsByStratum, matchedScaleConditions);
        var polaritySum = drawResult.DrawnCards.Sum(item => item.Card.PolarityWeight);
        var hasDivineBalance = drawResult.DrawnCards.Any(item =>
            doctrine.Polarity.DivineBalanceCardIds.Contains(item.Card.Id, StringComparer.OrdinalIgnoreCase));
        var eraState = DetermineEraState(polaritySum, doctrine.Polarity, hasDivineBalance);
        var dominantElement = DetermineDominantElement(drawResult.DrawnCards);
        var escalationReasons = DetermineEscalationReasons(countsByStratum);
        var moderationReasons = DetermineModerationReasons(drawResult.DrawnCards, countsByStratum, hasDivineBalance);
        var hasCalamity = countsByStratum.TryGetValue("Calamity", out var calamityCount) && calamityCount > 0;

        foreach (var drawnCard in drawResult.DrawnCards)
        {
            drawnCard.UsedShadow = ShouldUseShadow(drawnCard.Card, eraState, hasCalamity);
            drawnCard.SelectedMeaning = drawnCard.UsedShadow ? drawnCard.Card.Shadow : drawnCard.Card.Core;
            drawnCard.SelectedNote = eraState switch
            {
                "Astral" => drawnCard.Card.AstralNote,
                "Umbral" => drawnCard.Card.UmbralNote,
                _ => $"{drawnCard.Card.AstralNote} {drawnCard.Card.UmbralNote}".Trim()
            };
        }

        return new ReadingResult
        {
            SpreadName = drawResult.Spread.Name,
            OutputModeId = outputModeId,
            Question = question,
            Header = new ReadingHeader
            {
                Scale = scale,
                EraState = eraState,
                DominantElement = dominantElement,
                Escalation = escalationReasons.Count > 0,
                EscalationReasons = escalationReasons,
                Moderation = moderationReasons.Count > 0,
                ModerationReasons = moderationReasons
            },
            DrawnCards = drawResult.DrawnCards,
            Breakdown = BuildBreakdown(drawResult, doctrine, eraState, dominantElement),
            Diagnostics = new ReadingDiagnostics
            {
                PolaritySum = polaritySum,
                CountsByStratum = countsByStratum,
                CountsByElement = countsByElement,
                MatchedScaleConditions = matchedScaleConditions
            },
            Seed = drawResult.Seed
        };
    }

    private static string DetermineScale(
        DrawResult drawResult,
        Doctrine doctrine,
        IReadOnlyDictionary<string, int> countsByStratum,
        List<string> matchedScaleConditions)
    {
        if (TryMatchAnyRule("Era", doctrine.ScaleRules.Era.ConditionsAny, drawResult, countsByStratum, matchedScaleConditions))
        {
            return "Era";
        }

        if (TryMatchAnyRule("Major", doctrine.ScaleRules.Major.ConditionsAny, drawResult, countsByStratum, matchedScaleConditions))
        {
            return "Major";
        }

        if (TryMatchAllRule("Minor", doctrine.ScaleRules.Minor.Conditions, drawResult, countsByStratum, matchedScaleConditions))
        {
            return "Minor";
        }

        return "Minor";
    }

    private static bool TryMatchAnyRule(
        string label,
        IReadOnlyList<ScaleCondition> conditions,
        DrawResult drawResult,
        IReadOnlyDictionary<string, int> countsByStratum,
        List<string> matchedScaleConditions)
    {
        foreach (var condition in conditions)
        {
            if (EvaluateCondition(condition, drawResult, countsByStratum))
            {
                matchedScaleConditions.Add($"{label}: {DescribeCondition(condition)}");
                return true;
            }
        }

        return false;
    }

    private static bool TryMatchAllRule(
        string label,
        IReadOnlyList<ScaleCondition> conditions,
        DrawResult drawResult,
        IReadOnlyDictionary<string, int> countsByStratum,
        List<string> matchedScaleConditions)
    {
        if (conditions.Count == 0)
        {
            return false;
        }

        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition, drawResult, countsByStratum))
            {
                return false;
            }
        }

        matchedScaleConditions.AddRange(conditions.Select(condition => $"{label}: {DescribeCondition(condition)}"));
        return true;
    }

    private static bool EvaluateCondition(
        ScaleCondition condition,
        DrawResult drawResult,
        IReadOnlyDictionary<string, int> countsByStratum)
    {
        var axisCard = drawResult.DrawnCards.First(item => item.Position.Index == drawResult.Spread.AxisIndex).Card;

        if (!string.IsNullOrWhiteSpace(condition.AxisIsStratum) &&
            !string.Equals(axisCard.Stratum, condition.AxisIsStratum, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.AxisNotStratum) &&
            string.Equals(axisCard.Stratum, condition.AxisNotStratum, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.StratumPresent) &&
            !countsByStratum.ContainsKey(condition.StratumPresent))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.StratumAbsent) &&
            countsByStratum.ContainsKey(condition.StratumAbsent))
        {
            return false;
        }

        if (condition.MinCountByStratum is not null)
        {
            foreach (var pair in condition.MinCountByStratum)
            {
                if (!countsByStratum.TryGetValue(pair.Key, out var count) || count < pair.Value)
                {
                    return false;
                }
            }
        }

        if (condition.MaxCountByStratum is not null)
        {
            foreach (var pair in condition.MaxCountByStratum)
            {
                var count = countsByStratum.TryGetValue(pair.Key, out var value) ? value : 0;
                if (count > pair.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string DescribeCondition(ScaleCondition condition)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(condition.AxisIsStratum))
        {
            parts.Add($"axisIsStratum={condition.AxisIsStratum}");
        }

        if (!string.IsNullOrWhiteSpace(condition.AxisNotStratum))
        {
            parts.Add($"axisNotStratum={condition.AxisNotStratum}");
        }

        if (!string.IsNullOrWhiteSpace(condition.StratumPresent))
        {
            parts.Add($"stratumPresent={condition.StratumPresent}");
        }

        if (!string.IsNullOrWhiteSpace(condition.StratumAbsent))
        {
            parts.Add($"stratumAbsent={condition.StratumAbsent}");
        }

        if (condition.MinCountByStratum is not null)
        {
            parts.AddRange(condition.MinCountByStratum.Select(pair => $"minCountByStratum.{pair.Key}>={pair.Value}"));
        }

        if (condition.MaxCountByStratum is not null)
        {
            parts.AddRange(condition.MaxCountByStratum.Select(pair => $"maxCountByStratum.{pair.Key}<={pair.Value}"));
        }

        return string.Join(", ", parts);
    }

    private static string DetermineEraState(int polaritySum, PolarityRules rules, bool hasDivineBalance)
    {
        var eraState = polaritySum >= rules.AstralThreshold
            ? "Astral"
            : polaritySum <= rules.UmbralThreshold
                ? "Umbral"
                : "Transitional";

        if (!hasDivineBalance || !rules.StabilizeByOneTierIfBalancePresent)
        {
            return eraState;
        }

        return eraState switch
        {
            "Astral" => "Transitional",
            "Umbral" => "Transitional",
            _ => "Transitional"
        };
    }

    private static string? DetermineDominantElement(IReadOnlyList<DrawnCard> drawnCards)
    {
        var elementCounts = drawnCards
            .Where(item =>
                (!string.IsNullOrWhiteSpace(item.Card.Element)) &&
                (string.Equals(item.Card.Stratum, "Element", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(item.Card.Stratum, "Court", StringComparison.OrdinalIgnoreCase)))
            .GroupBy(item => item.Card.Element!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new { Element = group.Key, Count = group.Count() })
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Element, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (elementCounts.Count == 0)
        {
            return null;
        }

        if (elementCounts.Count > 1 && elementCounts[0].Count == elementCounts[1].Count)
        {
            return null;
        }

        return elementCounts[0].Element;
    }

    private static List<string> DetermineEscalationReasons(IReadOnlyDictionary<string, int> countsByStratum)
    {
        var reasons = new List<string>();

        if (countsByStratum.TryGetValue("Primal", out var primalCount) && primalCount >= 2)
        {
            reasons.Add("Two or more Primal cards are present.");
        }

        if (countsByStratum.TryGetValue("Calamity", out var calamityCount) && calamityCount > 0)
        {
            reasons.Add("A Calamity card is present.");
        }

        return reasons;
    }

    private static List<string> DetermineModerationReasons(
        IReadOnlyList<DrawnCard> drawnCards,
        IReadOnlyDictionary<string, int> countsByStratum,
        bool hasDivineBalance)
    {
        var reasons = new List<string>();

        if (countsByStratum.TryGetValue("Divine", out var divineCount) && divineCount >= 2)
        {
            reasons.Add("Two or more Divine cards are present.");
        }

        if (hasDivineBalance)
        {
            reasons.Add("A divine balance card is present.");
        }

        if (drawnCards.Any(item => item.Card.Flags.Contains("Moderate", StringComparer.OrdinalIgnoreCase)))
        {
            reasons.Add("Moderating flags are present in the draw.");
        }

        return reasons;
    }

    private static bool ShouldUseShadow(Card card, string eraState, bool hasCalamity)
    {
        if (string.Equals(eraState, "Umbral", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(card.Polarity, "Astral", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(eraState, "Astral", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(card.Polarity, "Umbral", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (hasCalamity && !string.Equals(card.Stratum, "Divine", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<ReadingBreakdownSection> BuildBreakdown(
        DrawResult drawResult,
        Doctrine doctrine,
        string eraState,
        string? dominantElement)
    {
        var axis = drawResult.DrawnCards.First(item => item.Position.Index == drawResult.Spread.AxisIndex);
        var highestAuthority = doctrine.AuthorityOrder
            .Select(stratum => drawResult.DrawnCards.Where(item => string.Equals(item.Card.Stratum, stratum, StringComparison.OrdinalIgnoreCase)).ToArray())
            .FirstOrDefault(cards => cards.Length > 0)
            ?? Array.Empty<DrawnCard>();
        var authorityText = highestAuthority.Length > 0
            ? $"{highestAuthority[0].Card.Stratum} holds the highest authority in this draw, with {string.Join(", ", highestAuthority.Select(item => item.Card.Name))} shaping the reading."
            : "No higher authority stratum is present beyond the elemental layer.";
        var amplificationCards = drawResult.DrawnCards
            .Where(item =>
                string.Equals(item.Card.Stratum, "Primal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Card.Stratum, "Shard", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Card.Name)
            .ToArray();
        var personaCards = drawResult.DrawnCards
            .Where(item => string.Equals(item.Card.Stratum, "Persona", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Card.Name)
            .ToArray();
        var manifestationCards = drawResult.DrawnCards
            .Where(item =>
                string.Equals(item.Card.Stratum, "Court", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Card.Stratum, "Element", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Card.Name)
            .ToArray();

        return new[]
        {
            new ReadingBreakdownSection
            {
                Title = "Cosmic Authority",
                Summary = $"{authorityText} The axis card is {axis.Card.Name}, which suggests the core tension is being named directly."
            },
            new ReadingBreakdownSection
            {
                Title = "Amplification & Distortion",
                Summary = amplificationCards.Length > 0
                    ? $"Primal and Shard pressures appear through {string.Join(", ", amplificationCards)}, indicating amplification is part of the pattern."
                    : "No Primal or Shard layer is present, indicating amplification and distortion are limited."
            },
            new ReadingBreakdownSection
            {
                Title = "Personal Lens",
                Summary = personaCards.Length > 0
                    ? $"Persona cards {string.Join(", ", personaCards)} indicate the reading passes through a defined personal lens."
                    : "No Persona card is present, suggesting the reading remains less personalized than usual."
            },
            new ReadingBreakdownSection
            {
                Title = "Manifestation",
                Summary = manifestationCards.Length > 0
                    ? $"The manifest layer appears through {string.Join(", ", manifestationCards)}{(dominantElement is null ? "." : $", with {dominantElement} acting as the dominant element.")}"
                    : "The manifest layer is limited in this draw."
            },
            new ReadingBreakdownSection
            {
                Title = "Recommendation",
                Summary = eraState switch
                {
                    "Astral" => "The era state suggests action, but action should remain proportionate to the strongest authority cards.",
                    "Umbral" => "The era state suggests reflection, conservation, and a measured pace before commitment.",
                    _ => "The era state suggests balancing action with reflection until the pattern clarifies further."
                }
            }
        };
    }
}
