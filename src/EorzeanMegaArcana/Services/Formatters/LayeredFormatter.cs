using EorzeanMegaArcana.Models;
using System.Text;

namespace EorzeanMegaArcana.Services.Formatters;

public sealed class LayeredFormatter : IReadingFormatter
{
    private static readonly string[] OrderedHeaders =
    {
        "Cosmic Authority",
        "Amplification & Distortion",
        "Personal Lens",
        "Manifestation",
        "Recommendation"
    };

    public LayeredFormatter(string interpretationBias = InterpretationBiasOptions.Auto)
    {
    }

    public string Format(ReadingResult result, Doctrine doctrine)
    {
        var builder = new StringBuilder();

        var sections = (result.Breakdown ?? Array.Empty<ReadingBreakdownSection>())
            .ToDictionary(section => section.Title, StringComparer.OrdinalIgnoreCase);

        foreach (var header in OrderedHeaders)
        {
            builder.AppendLine(header);
            builder.AppendLine(sections.TryGetValue(header, out var section)
                ? section.Summary
                : "No summary is available for this layer.");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
