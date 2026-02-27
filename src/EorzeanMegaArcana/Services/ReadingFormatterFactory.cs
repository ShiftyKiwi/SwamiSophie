using EorzeanMegaArcana.Services.Formatters;

namespace EorzeanMegaArcana.Services;

public static class ReadingFormatterFactory
{
    public static IReadingFormatter Create(string outputModeId, string interpretationBias = Models.InterpretationBiasOptions.Auto)
    {
        return outputModeId.ToLowerInvariant() switch
        {
            "layered" => new LayeredFormatter(interpretationBias),
            "scholarly" => new ScholarlyFormatter(interpretationBias),
            _ => new ConciseFormatter(interpretationBias)
        };
    }
}
