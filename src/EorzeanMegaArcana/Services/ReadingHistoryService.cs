using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class ReadingHistoryService
{
    private readonly List<ReadingHistoryEntry> entries = new();

    public int Capacity { get; } = 50;

    public IReadOnlyList<ReadingHistoryEntry> Entries => this.entries;

    public void Add(ReadingHistoryEntry entry)
    {
        this.entries.Insert(0, entry);
        TrimToCapacity();
    }

    public void Clear()
    {
        this.entries.Clear();
    }

    public void LoadPersistedEntries(IEnumerable<PersistedReadingHistoryEntry>? persistedEntries)
    {
        this.entries.Clear();
        if (persistedEntries is null)
        {
            return;
        }

        foreach (var entry in persistedEntries
            .Where(IsValid)
            .OrderByDescending(item => item.Timestamp)
            .Take(this.Capacity))
        {
            this.entries.Add(new ReadingHistoryEntry
            {
                EntryId = entry.EntryId == Guid.Empty ? Guid.NewGuid() : entry.EntryId,
                Timestamp = entry.Timestamp == default ? DateTimeOffset.UtcNow : entry.Timestamp,
                Question = entry.Question,
                SpreadId = entry.SpreadId,
                SpreadName = entry.SpreadName,
                OutputModeId = entry.OutputModeId,
                SeedUsed = entry.SeedUsed,
                Seed = entry.Seed,
                AllowRepeats = entry.AllowRepeats,
                DrawnCardIds = entry.DrawnCardIds.ToArray(),
                Scale = entry.Scale,
                EraState = entry.EraState,
                CoreNarrativePreview = entry.CoreNarrativePreview
            });
        }

        TrimToCapacity();
    }

    public IReadOnlyList<PersistedReadingHistoryEntry> ToPersistedEntries()
    {
        return this.entries
            .Take(this.Capacity)
            .Select(entry => new PersistedReadingHistoryEntry
            {
                EntryId = entry.EntryId,
                Timestamp = entry.Timestamp,
                Question = entry.Question,
                SpreadId = entry.SpreadId,
                SpreadName = entry.SpreadName,
                OutputModeId = entry.OutputModeId,
                SeedUsed = entry.SeedUsed,
                Seed = entry.Seed,
                AllowRepeats = entry.AllowRepeats,
                DrawnCardIds = entry.DrawnCardIds.ToList(),
                Scale = entry.Scale,
                EraState = entry.EraState,
                CoreNarrativePreview = entry.CoreNarrativePreview
            })
            .ToArray();
    }

    private void TrimToCapacity()
    {
        if (this.entries.Count <= this.Capacity)
        {
            return;
        }

        this.entries.RemoveRange(this.Capacity, this.entries.Count - this.Capacity);
    }

    private static bool IsValid(PersistedReadingHistoryEntry? entry)
    {
        return entry is not null
            && !string.IsNullOrWhiteSpace(entry.SpreadId)
            && !string.IsNullOrWhiteSpace(entry.OutputModeId)
            && entry.DrawnCardIds is { Count: > 0 }
            && entry.DrawnCardIds.All(id => !string.IsNullOrWhiteSpace(id));
    }
}
