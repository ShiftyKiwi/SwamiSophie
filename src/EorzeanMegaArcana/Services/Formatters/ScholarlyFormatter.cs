using EorzeanMegaArcana.Models;
using System.Text;

namespace EorzeanMegaArcana.Services.Formatters;

public sealed class ScholarlyFormatter : IReadingFormatter
{
    private readonly string interpretationBias;

    public ScholarlyFormatter(string interpretationBias = InterpretationBiasOptions.Auto)
    {
        this.interpretationBias = InterpretationBiasOptions.Normalize(interpretationBias);
    }

    public string Format(ReadingResult result, Doctrine doctrine)
    {
        var builder = new StringBuilder();
        builder.AppendLine(new LayeredFormatter(this.interpretationBias).Format(result, doctrine));
        builder.AppendLine();
        builder.AppendLine("Authority Order Used");
        builder.AppendLine(string.Join(" > ", doctrine.AuthorityOrder));
        builder.AppendLine();
        builder.AppendLine("Scale Conditions Matched");
        builder.AppendLine(result.Diagnostics.MatchedScaleConditions.Count > 0
            ? string.Join(Environment.NewLine, result.Diagnostics.MatchedScaleConditions)
            : "No explicit scale conditions matched; Minor was used as the doctrinal fallback.");
        builder.AppendLine();
        builder.AppendLine("Diagnostics");
        builder.AppendLine("Polarity Sum and Thresholds");
        builder.AppendLine($"Sum: {result.Diagnostics.PolaritySum}; Astral >= {doctrine.Polarity.AstralThreshold}; Umbral <= {doctrine.Polarity.UmbralThreshold}");
        builder.AppendLine();
        builder.AppendLine("Counts By Stratum");
        foreach (var pair in result.Diagnostics.CountsByStratum.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"{pair.Key}: {pair.Value}");
        }

        builder.AppendLine();
        builder.AppendLine("Counts By Element");
        if (result.Diagnostics.CountsByElement.Count == 0)
        {
            builder.AppendLine("None");
        }
        else
        {
            foreach (var pair in result.Diagnostics.CountsByElement.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"{pair.Key}: {pair.Value}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
