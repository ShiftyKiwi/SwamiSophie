namespace EorzeanMegaArcana.Models;

public sealed class ReadingRequest
{
    public SpreadDefinition Spread { get; set; } = new();

    public string OutputModeId { get; set; } = "concise";

    public string? Question { get; set; }

    public int? Seed { get; set; }

    public bool AllowRepeats { get; set; }
}
