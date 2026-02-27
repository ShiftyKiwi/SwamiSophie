using System.Text.Json.Serialization;

namespace EorzeanMegaArcana.Models;

public sealed class SpreadDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cardCount")]
    public int CardCount { get; set; }

    [JsonPropertyName("layout")]
    public string Layout { get; set; } = string.Empty;

    [JsonPropertyName("axisIndex")]
    public int AxisIndex { get; set; }

    [JsonPropertyName("positions")]
    public IReadOnlyList<PositionDefinition> Positions { get; set; } = Array.Empty<PositionDefinition>();
}

public sealed class PositionDefinition
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
