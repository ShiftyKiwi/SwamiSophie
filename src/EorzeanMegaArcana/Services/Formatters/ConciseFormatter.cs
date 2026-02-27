using EorzeanMegaArcana.Models;
using System.Text;

namespace EorzeanMegaArcana.Services.Formatters;

public sealed class ConciseFormatter : IReadingFormatter
{
    private readonly string interpretationBias;

    public ConciseFormatter(string interpretationBias = InterpretationBiasOptions.Auto)
    {
        this.interpretationBias = InterpretationBiasOptions.Normalize(interpretationBias);
    }

    public string Format(ReadingResult result, Doctrine doctrine)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Core Narrative");
        builder.AppendLine(BuildCoreNarrative(result, doctrine));
        builder.AppendLine();

        if (result.DrawnCards.Count == 3)
        {
            builder.AppendLine("Pressure");
            builder.AppendLine(DescribePressure(result, doctrine));
            builder.AppendLine();
            builder.AppendLine("Axis");
            builder.AppendLine(DescribeAxis(result));
            builder.AppendLine();
            builder.AppendLine("Direction");
            builder.AppendLine(DescribeDirection(result));
        }
        else
        {
            builder.AppendLine("First Row");
            builder.AppendLine(DescribeRow(result, 1, 3));
            builder.AppendLine();
            builder.AppendLine("Second Row");
            builder.AppendLine(DescribeRow(result, 4, 6));
            builder.AppendLine();
            builder.AppendLine("Third Row");
            builder.AppendLine(DescribeRow(result, 7, 9));
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildCoreNarrative(ReadingResult result, Doctrine doctrine)
    {
        var summary = new StringBuilder();
        summary.Append($"This reading suggests a {result.Header.Scale.ToLowerInvariant()} scale pattern in a {result.Header.EraState.ToLowerInvariant()} era state. ");
        summary.Append(DescribeAuthority(result, doctrine));
        summary.Append(' ');
        summary.Append(DescribeDirection(result));
        return summary.ToString().Trim();
    }

    private static string DescribeAuthority(ReadingResult result, Doctrine doctrine)
    {
        foreach (var stratum in doctrine.AuthorityOrder)
        {
            var cards = result.DrawnCards.Where(item => string.Equals(item.Card.Stratum, stratum, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (cards.Length > 0)
            {
                return $"{stratum} appears as the highest active authority through {string.Join(", ", cards.Select(item => item.Card.Name))}, which indicates the reading should be weighed from that layer first.";
            }
        }

        return "The draw remains concentrated in the lower manifest layers, which suggests practical conditions deserve the closest attention.";
    }

    private string DescribePressure(ReadingResult result, Doctrine doctrine)
    {
        var pressure = result.DrawnCards.OrderBy(item => item.Position.Index).First();
        var selectedMeaning = FormatterMeaningSelector.SelectMeaning(result, pressure, this.interpretationBias);
        return $"{pressure.Card.Name} frames the immediate pressure. {selectedMeaning} {DescribeAuthority(result, doctrine)}".Trim();
    }

    private string DescribeAxis(ReadingResult result)
    {
        var axis = result.DrawnCards.First(item => item.Position.Name.Equals("Axis", StringComparison.OrdinalIgnoreCase));
        var selectedMeaning = FormatterMeaningSelector.SelectMeaning(result, axis, this.interpretationBias);
        var selectedNote = FormatterMeaningSelector.SelectNote(result, axis);
        return $"{axis.Card.Name} occupies the axis, and its selected meaning points toward this emphasis: {selectedMeaning} {selectedNote}".Trim();
    }

    private static string DescribeDirection(ReadingResult result)
    {
        var recommendation = result.Header.EraState switch
        {
            "Astral" => "The pattern points toward measured action rather than delay.",
            "Umbral" => "The pattern points toward reflection, protection, and reduced velocity.",
            _ => "The pattern points toward balance, revision, and careful pacing."
        };

        return result.Header.DominantElement is null
            ? recommendation
            : $"{recommendation} The dominant elemental influence is {result.Header.DominantElement}.";
    }

    private string DescribeRow(ReadingResult result, int startIndex, int endIndex)
    {
        var row = result.DrawnCards
            .Where(item => item.Position.Index >= startIndex && item.Position.Index <= endIndex)
            .OrderBy(item => item.Position.Index)
            .ToArray();

        if (row.Length == 0)
        {
            return "No row data is present.";
        }

        var names = string.Join(", ", row.Select(item => item.Card.Name));
        var emphasis = string.Join(" ", row.Select(item => FormatterMeaningSelector.SelectMeaning(result, item, this.interpretationBias)));
        return $"{names} suggest a shared row emphasis. {emphasis}".Trim();
    }
}
