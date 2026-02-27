using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public static class ReadingTextFormatter
{
    public static string BuildCoreNarrativePreview(string narrative, int maxLength = 60)
    {
        var sections = ParseConciseSections(narrative);
        var preview = sections.TryGetValue("Core Narrative", out var coreNarrative)
            ? coreNarrative
            : string.Empty;

        if (preview.Length <= maxLength)
        {
            return preview;
        }

        return preview[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }

    public static string BuildSummaryText(
        ReadingResult result,
        Doctrine doctrine,
        string interpretationBias,
        DateTimeOffset? timestamp = null)
    {
        var conciseNarrative = ReadingFormatterFactory.Create("concise", interpretationBias).Format(result, doctrine);
        var sections = ParseConciseSections(conciseNarrative);
        var localTimestamp = (timestamp ?? DateTimeOffset.Now).ToLocalTime();
        var builder = new System.Text.StringBuilder();

        builder.AppendLine("Swami Sophie — Reading Summary");
        builder.AppendLine();
        builder.AppendLine($"Timestamp: {localTimestamp:G}");
        builder.AppendLine();
        builder.AppendLine($"Spread: {result.SpreadName} | Output: {result.OutputModeId} | Bias: {InterpretationBiasOptions.Normalize(interpretationBias)}");

        if (!string.IsNullOrWhiteSpace(result.Question))
        {
            builder.AppendLine();
            builder.AppendLine($"Question: {result.Question}");
        }

        builder.AppendLine();
        builder.AppendLine($"Scale: {result.Header.Scale} | Era: {result.Header.EraState} | Dominant: {result.Header.DominantElement ?? "None"} | Escalation: {(result.Header.Escalation ? "Yes" : "No")} | Moderation: {(result.Header.Moderation ? "Yes" : "No")}");
        builder.AppendLine();
        builder.AppendLine($"Core Narrative: {sections.GetValueOrDefault("Core Narrative", string.Empty)}");
        builder.AppendLine();

        if (result.DrawnCards.Count == 3)
        {
            builder.AppendLine($"Pressure: {sections.GetValueOrDefault("Pressure", string.Empty)}");
            builder.AppendLine();
            builder.AppendLine($"Axis: {sections.GetValueOrDefault("Axis", string.Empty)}");
            builder.AppendLine();
            builder.AppendLine($"Direction: {sections.GetValueOrDefault("Direction", string.Empty)}");
        }
        else
        {
            builder.AppendLine($"First Row: {sections.GetValueOrDefault("First Row", string.Empty)}");
            builder.AppendLine();
            builder.AppendLine($"Second Row: {sections.GetValueOrDefault("Second Row", string.Empty)}");
            builder.AppendLine();
            builder.AppendLine($"Third Row: {sections.GetValueOrDefault("Third Row", string.Empty)}");
        }

        builder.AppendLine();
        builder.AppendLine("— End —");
        return builder.ToString().TrimEnd();
    }

    public static string BuildExportText(ReadingResult result, DateTimeOffset? timestamp = null)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"Question: {result.Question ?? "(none)"}");
        builder.AppendLine($"Date/Time: {(timestamp ?? DateTimeOffset.UtcNow):O}");
        builder.AppendLine($"Spread: {result.SpreadName}");
        builder.AppendLine($"Output Mode: {result.OutputModeId}");
        builder.AppendLine($"Scale: {result.Header.Scale}");
        builder.AppendLine($"Era State: {result.Header.EraState}");
        builder.AppendLine($"Dominant Element: {result.Header.DominantElement ?? "None"}");
        builder.AppendLine($"Escalation: {(result.Header.Escalation ? "Yes" : "No")} - {JoinOrNone(result.Header.EscalationReasons)}");
        builder.AppendLine($"Moderation: {(result.Header.Moderation ? "Yes" : "No")} - {JoinOrNone(result.Header.ModerationReasons)}");
        builder.AppendLine();
        builder.AppendLine("Positions");

        foreach (var card in result.DrawnCards.OrderBy(item => item.Position.Index))
        {
            builder.AppendLine($"{card.Position.Name}: {card.Card.Name}");
        }

        builder.AppendLine();
        builder.AppendLine("Output");
        builder.AppendLine(result.Narrative);

        if (string.Equals(result.OutputModeId, "scholarly", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.AppendLine("Diagnostics");
            builder.AppendLine($"Polarity Sum: {result.Diagnostics.PolaritySum}");
            builder.AppendLine($"Counts By Stratum: {JoinPairs(result.Diagnostics.CountsByStratum)}");
            builder.AppendLine($"Counts By Element: {JoinPairs(result.Diagnostics.CountsByElement)}");
        }

        return builder.ToString().TrimEnd();
    }

    private static Dictionary<string, string> ParseConciseSections(string narrative)
    {
        var lines = narrative.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var knownHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Core Narrative",
            "Pressure",
            "Axis",
            "Direction",
            "First Row",
            "Second Row",
            "Third Row"
        };

        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentHeader = null;
        var currentContent = new List<string>();

        foreach (var line in lines)
        {
            if (knownHeaders.Contains(line))
            {
                if (currentHeader is not null)
                {
                    sections[currentHeader] = string.Join(" ", currentContent).Trim();
                }

                currentHeader = line;
                currentContent.Clear();
                continue;
            }

            if (currentHeader is not null)
            {
                currentContent.Add(line);
            }
        }

        if (currentHeader is not null)
        {
            sections[currentHeader] = string.Join(" ", currentContent).Trim();
        }

        return sections;
    }

    private static string JoinOrNone(IReadOnlyList<string> values) => values.Count == 0 ? "None" : string.Join("; ", values);

    private static string JoinPairs(IReadOnlyDictionary<string, int> values) =>
        values.Count == 0 ? "None" : string.Join(", ", values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));
}
