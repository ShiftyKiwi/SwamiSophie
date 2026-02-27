using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class DataBundle
{
    public IReadOnlyList<Card> AllCards { get; set; } = Array.Empty<Card>();

    public IReadOnlyDictionary<string, SpreadDefinition> SpreadsById { get; set; } =
        new Dictionary<string, SpreadDefinition>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<OutputModeDefinition> OutputModes { get; set; } = Array.Empty<OutputModeDefinition>();

    public Doctrine Doctrine { get; set; } = new();
}
