using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class ReadingHistoryTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void RestoringSavedDrawReproducesCardIdsAndNarrative()
    {
        var service = CreateReadingService();
        var draw = service.Draw("aether-pulse", allowRepeats: false, seed: 77);
        var reading = service.Interpret(draw, "layered", "What pressure surrounds this choice?");

        var entry = new ReadingHistoryEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Question = reading.Question,
            SpreadId = draw.Spread.Id,
            SpreadName = draw.Spread.Name,
            OutputModeId = reading.OutputModeId,
            SeedUsed = true,
            Seed = draw.Seed,
            AllowRepeats = false,
            DrawnCardIds = draw.DrawnCards.OrderBy(item => item.Position.Index).Select(item => item.Card.Id).ToArray(),
            Scale = reading.Header.Scale,
            EraState = reading.Header.EraState,
            CoreNarrativePreview = ReadingTextFormatter.BuildCoreNarrativePreview(reading.Narrative),
            ReadingResult = reading
        };

        var restoredDraw = service.RestoreDraw(entry.SpreadId, entry.DrawnCardIds, entry.Seed);
        var restoredIds = restoredDraw.DrawnCards.OrderBy(item => item.Position.Index).Select(item => item.Card.Id).ToArray();
        var restoredReading = service.Interpret(restoredDraw, entry.OutputModeId, entry.Question);

        Assert.Equal(entry.DrawnCardIds, restoredIds);
        Assert.False(string.IsNullOrWhiteSpace(restoredReading.Narrative));
    }

    [Fact]
    public void PersistedHistoryRoundTripKeepsLightweightDataOnly()
    {
        var historyService = new ReadingHistoryService();
        var service = CreateReadingService();
        var draw = service.Draw("aether-pulse", allowRepeats: false, seed: 91);
        var reading = service.Interpret(draw, "concise", "What holds steady?");

        historyService.Add(new ReadingHistoryEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Question = reading.Question,
            SpreadId = draw.Spread.Id,
            SpreadName = draw.Spread.Name,
            OutputModeId = reading.OutputModeId,
            SeedUsed = true,
            Seed = draw.Seed,
            AllowRepeats = false,
            DrawnCardIds = draw.DrawnCards.OrderBy(item => item.Position.Index).Select(item => item.Card.Id).ToArray(),
            Scale = reading.Header.Scale,
            EraState = reading.Header.EraState,
            CoreNarrativePreview = ReadingTextFormatter.BuildCoreNarrativePreview(reading.Narrative),
            ReadingResult = reading
        });

        var persisted = historyService.ToPersistedEntries();
        var restoredHistory = new ReadingHistoryService();
        restoredHistory.LoadPersistedEntries(persisted);
        var restoredEntry = Assert.Single(restoredHistory.Entries);

        Assert.Null(restoredEntry.ReadingResult);
        Assert.Equal(draw.DrawnCards.OrderBy(item => item.Position.Index).Select(item => item.Card.Id), restoredEntry.DrawnCardIds);

        var restoredDraw = service.RestoreDraw(restoredEntry.SpreadId, restoredEntry.DrawnCardIds, restoredEntry.Seed);
        var restoredReading = service.Interpret(restoredDraw, restoredEntry.OutputModeId, restoredEntry.Question);

        Assert.False(string.IsNullOrWhiteSpace(restoredReading.Narrative));
    }

    [Fact]
    public void LoadingCorruptedPersistedHistoryClearsGracefully()
    {
        var historyService = new ReadingHistoryService();
        historyService.LoadPersistedEntries(
        [
            new PersistedReadingHistoryEntry { SpreadId = string.Empty, OutputModeId = "concise", DrawnCardIds = new List<string> { "fire-i" } },
            new PersistedReadingHistoryEntry { SpreadId = "aether-pulse", OutputModeId = string.Empty, DrawnCardIds = new List<string> { "fire-i" } }
        ]);

        Assert.Empty(historyService.Entries);
    }

    private static ReadingService CreateReadingService()
    {
        var dataRoot = Path.Combine(RepoRoot, "data");
        var dataBundle = new DataLoader(dataRoot).Load();
        return new ReadingService(
            dataBundle,
            new DeckService(dataBundle.AllCards),
            new DrawService(),
            new InterpretationEngine());
    }
}
