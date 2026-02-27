using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using System.Numerics;

namespace EorzeanMegaArcana.UI.Windows;

public sealed class HistoryWindow : Window
{
    private readonly ReadingHistoryService historyService;
    private readonly ReadingService? readingService;
    private readonly Action<ReadingHistoryEntry> reopenEntry;
    private Guid? selectedEntryId;

    public HistoryWindow(ReadingHistoryService historyService, ReadingService? readingService, Action<ReadingHistoryEntry> reopenEntry)
        : base("Reading History###SwamiSophieHistoryWindow")
    {
        this.historyService = historyService;
        this.readingService = readingService;
        this.reopenEntry = reopenEntry;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(960, 560),
            MaximumSize = new Vector2(1800, 1400)
        };
    }

    public override void Draw()
    {
        if (ImGui.BeginTable("HistoryLayout", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Entries", ImGuiTableColumnFlags.WidthStretch, 0.95f);
            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch, 1.05f);
            ImGui.TableNextColumn();
            DrawEntries();
            ImGui.TableNextColumn();
            DrawDetails();
            ImGui.EndTable();
        }
    }

    private void DrawEntries()
    {
        ImGui.BeginChild("HistoryEntries", new Vector2(0, 0), true);

        if (this.historyService.Entries.Count == 0)
        {
            ImGui.TextDisabled("No readings recorded yet.");
            ImGui.EndChild();
            return;
        }

        foreach (var entry in this.historyService.Entries)
        {
            var selected = this.selectedEntryId == entry.EntryId;
            if (ImGui.Selectable(BuildRowLabel(entry), selected))
            {
                this.selectedEntryId = entry.EntryId;
            }
        }

        ImGui.EndChild();
    }

    private void DrawDetails()
    {
        var entry = this.historyService.Entries.FirstOrDefault(item => item.EntryId == this.selectedEntryId);

        ImGui.BeginChild("HistoryDetails", new Vector2(0, 0), true);
        if (entry is null)
        {
            ImGui.TextDisabled("Select a history entry to inspect it.");
            ImGui.EndChild();
            return;
        }

        var resolvedReading = ResolveReading(entry);

        ImGui.TextWrapped(entry.SpreadName);
        ImGui.TextDisabled(entry.Timestamp.LocalDateTime.ToString("g"));
        ImGui.Separator();
        ImGui.TextWrapped($"Spread Id: {entry.SpreadId}");
        ImGui.TextWrapped($"Output Mode: {entry.OutputModeId}");
        ImGui.TextWrapped($"Scale: {entry.Scale}");
        ImGui.TextWrapped($"Era State: {entry.EraState}");
        ImGui.TextWrapped($"Question: {entry.Question ?? "(none)"}");
        ImGui.TextWrapped($"Seed Used: {(entry.SeedUsed ? "Yes" : "No")}");
        ImGui.TextWrapped($"Seed: {(entry.Seed is int seed ? seed : "None")}");
        ImGui.TextWrapped($"Allow Repeats: {(entry.AllowRepeats ? "Yes" : "No")}");
        ImGui.Separator();

        if (resolvedReading is null)
        {
            ImGui.TextDisabled("Reading details are unavailable until the deck data can be resolved.");
        }
        else
        {
            ImGui.TextUnformatted("Drawn Cards");
            foreach (var drawnCard in resolvedReading.DrawnCards.OrderBy(item => item.Position.Index))
            {
                ImGui.BulletText($"{drawnCard.Position.Name}: {drawnCard.Card.Name} ({drawnCard.Card.Id})");
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Narrative");
            ImGui.TextWrapped(resolvedReading.Narrative);
        }

        if (ImGui.Button("Reopen"))
        {
            this.reopenEntry(entry);
        }

        if (resolvedReading is not null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Copy Reading"))
            {
                ImGui.SetClipboardText(ReadingTextFormatter.BuildExportText(resolvedReading, entry.Timestamp));
            }

            ImGui.SameLine();
            if (ImGui.Button("Export Reading"))
            {
                ExportEntry(entry, resolvedReading);
            }
        }

        ImGui.EndChild();
    }

    private ReadingResult? ResolveReading(ReadingHistoryEntry entry)
    {
        if (entry.ReadingResult is { Narrative.Length: > 0 })
        {
            return entry.ReadingResult;
        }

        if (this.readingService is null)
        {
            return null;
        }

        try
        {
            var draw = this.readingService.RestoreDraw(entry.SpreadId, entry.DrawnCardIds, entry.SeedUsed ? entry.Seed : null);
            entry.ReadingResult = this.readingService.Interpret(draw, entry.OutputModeId, entry.Question);
            return entry.ReadingResult;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildRowLabel(ReadingHistoryEntry entry)
    {
        return $"{entry.Timestamp.LocalDateTime:g} | {entry.SpreadName} | {entry.Scale} | {entry.EraState} | {entry.CoreNarrativePreview}";
    }

    private static void ExportEntry(ReadingHistoryEntry entry, ReadingResult reading)
    {
        var directory = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "SwamiSophieExports");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"history-reading-{entry.Timestamp.UtcDateTime:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, ReadingTextFormatter.BuildExportText(reading, entry.Timestamp));
    }
}
