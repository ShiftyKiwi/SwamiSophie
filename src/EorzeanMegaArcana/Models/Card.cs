using System.Text.Json.Serialization;

namespace EorzeanMegaArcana.Models;

public sealed class Card
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stratum")]
    public string Stratum { get; set; } = string.Empty;

    [JsonPropertyName("element")]
    public string? Element { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("polarity")]
    public string Polarity { get; set; } = string.Empty;

    [JsonPropertyName("polarityWeight")]
    public int PolarityWeight { get; set; }

    [JsonPropertyName("core")]
    public string Core { get; set; } = string.Empty;

    [JsonPropertyName("shadow")]
    public string Shadow { get; set; } = string.Empty;

    [JsonPropertyName("astralNote")]
    public string AstralNote { get; set; } = string.Empty;

    [JsonPropertyName("umbralNote")]
    public string UmbralNote { get; set; } = string.Empty;

    [JsonPropertyName("flags")]
    public IReadOnlyList<string> Flags { get; set; } = Array.Empty<string>();
}
