namespace EorzeanMegaArcana.Models;

public sealed class DrawResult
{
    public SpreadDefinition Spread { get; set; } = new();

    public IReadOnlyList<DrawnCard> DrawnCards { get; set; } = Array.Empty<DrawnCard>();

    public int? Seed { get; set; }
}

public sealed class DrawnCard
{
    public PositionDefinition Position { get; set; } = new();

    public Card Card { get; set; } = new();

    public bool UsedShadow { get; set; }

    public string SelectedMeaning { get; set; } = string.Empty;

    public string SelectedNote { get; set; } = string.Empty;
}

public sealed class ReadingHeader
{
    public string Scale { get; set; } = "Minor";

    public string EraState { get; set; } = "Transitional";

    public string? DominantElement { get; set; }

    public bool Escalation { get; set; }

    public IReadOnlyList<string> EscalationReasons { get; set; } = Array.Empty<string>();

    public bool Moderation { get; set; }

    public IReadOnlyList<string> ModerationReasons { get; set; } = Array.Empty<string>();
}

public sealed class ReadingBreakdownSection
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;
}

public sealed class ReadingDiagnostics
{
    public int PolaritySum { get; set; }

    public IReadOnlyDictionary<string, int> CountsByStratum { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> CountsByElement { get; set; } = new Dictionary<string, int>();

    public IReadOnlyList<string> MatchedScaleConditions { get; set; } = Array.Empty<string>();
}

public sealed class ReadingResult
{
    public string SpreadName { get; set; } = string.Empty;

    public string OutputModeId { get; set; } = "concise";

    public string? Question { get; set; }

    public ReadingHeader Header { get; set; } = new();

    public IReadOnlyList<DrawnCard> DrawnCards { get; set; } = Array.Empty<DrawnCard>();

    public string Narrative { get; set; } = string.Empty;

    public IReadOnlyList<ReadingBreakdownSection>? Breakdown { get; set; }

    public ReadingDiagnostics Diagnostics { get; set; } = new();

    public int? Seed { get; set; }
}
