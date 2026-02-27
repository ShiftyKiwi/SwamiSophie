namespace EorzeanMegaArcana.Models;

public sealed class ReadingHistoryEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string? Question { get; set; }

    public string SpreadId { get; set; } = string.Empty;

    public string SpreadName { get; set; } = string.Empty;

    public string OutputModeId { get; set; } = string.Empty;

    public bool SeedUsed { get; set; }

    public int? Seed { get; set; }

    public bool AllowRepeats { get; set; }

    public IReadOnlyList<string> DrawnCardIds { get; set; } = Array.Empty<string>();

    public string Scale { get; set; } = string.Empty;

    public string EraState { get; set; } = string.Empty;

    public string CoreNarrativePreview { get; set; } = string.Empty;

    public ReadingResult? ReadingResult { get; set; }
}

public sealed class PersistedReadingHistoryEntry
{
    public Guid EntryId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string? Question { get; set; }

    public string SpreadId { get; set; } = string.Empty;

    public string SpreadName { get; set; } = string.Empty;

    public string OutputModeId { get; set; } = string.Empty;

    public bool SeedUsed { get; set; }

    public int? Seed { get; set; }

    public bool AllowRepeats { get; set; }

    public List<string> DrawnCardIds { get; set; } = new();

    public string Scale { get; set; } = string.Empty;

    public string EraState { get; set; } = string.Empty;

    public string CoreNarrativePreview { get; set; } = string.Empty;
}
