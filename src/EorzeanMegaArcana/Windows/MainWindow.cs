using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeanMegaArcana.Models;
using EorzeanMegaArcana.Services;
using System.Numerics;

namespace EorzeanMegaArcana.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private const string ReplaceSpreadPopupId = "Replace the current spread?###SwamiSophieReplaceSpread";

    private readonly Configuration configuration;
    private readonly IPluginLogAdapter log;
    private readonly ReadingService? readingService;
    private readonly ReadingHistoryService historyService;
    private readonly string? loadError;
    private readonly Action toggleDeckBrowser;
    private readonly Action toggleHistoryWindow;

    private int spreadIndex;
    private int outputModeIndex;
    private int interpretationBiasIndex;
    private bool allowRepeats;
    private bool useSeed;
    private bool pinDraw;
    private int seedValue;
    private string question = string.Empty;
    private DrawResult? lastDraw;
    private ReadingResult? lastReading;
    private string statusMessage = "Ready.";
    private bool lastSeedUsed;
    private PendingDrawAction pendingDrawAction;

    public MainWindow(
        Configuration configuration,
        IPluginLogAdapter log,
        ReadingService? readingService,
        ReadingHistoryService historyService,
        string? loadError,
        Action toggleDeckBrowser,
        Action toggleHistoryWindow)
        : base("Swami Sophie###SwamiSophieMainWindow")
    {
        this.configuration = configuration;
        this.log = log;
        this.readingService = readingService;
        this.historyService = historyService;
        this.loadError = loadError;
        this.toggleDeckBrowser = toggleDeckBrowser;
        this.toggleHistoryWindow = toggleHistoryWindow;
        this.allowRepeats = configuration.DefaultAllowRepeats;
        this.useSeed = configuration.DefaultUseSeed;
        this.pinDraw = configuration.PinDraw;
        this.seedValue = configuration.SeedValue;
        this.interpretationBiasIndex = FindInterpretationBiasIndex(configuration.InterpretationBias);

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 560),
            MaximumSize = new Vector2(1600, 1200)
        };

        if (readingService is not null)
        {
            this.spreadIndex = FindSpreadIndex(configuration.DefaultSpreadId);
            this.outputModeIndex = FindOutputModeIndex(configuration.DefaultOutputModeId);
            this.readingService.InterpretationBias = InterpretationBiasOptions.All[this.interpretationBiasIndex];
        }
    }

    public bool PinDrawEnabled => this.pinDraw;

    public void Dispose()
    {
    }

    public void RestoreHistoryEntry(ReadingHistoryEntry entry)
    {
        if (this.readingService is null)
        {
            this.statusMessage = "Reading service is unavailable.";
            return;
        }

        try
        {
            this.question = entry.Question ?? string.Empty;
            this.allowRepeats = entry.AllowRepeats;
            this.useSeed = entry.SeedUsed;
            this.seedValue = entry.Seed ?? this.seedValue;
            this.lastSeedUsed = entry.SeedUsed;
            this.spreadIndex = FindSpreadIndex(entry.SpreadId);
            this.outputModeIndex = FindOutputModeIndex(entry.OutputModeId);
            this.lastDraw = this.readingService.RestoreDraw(entry.SpreadId, entry.DrawnCardIds, entry.SeedUsed ? entry.Seed : null);
            this.lastReading = this.readingService.Interpret(this.lastDraw, entry.OutputModeId, entry.Question);
            this.statusMessage = "History entry reopened.";
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Failed to reopen history entry.");
            this.statusMessage = ex.Message;
        }
    }

    public void SetPinDraw(bool enabled)
    {
        this.pinDraw = enabled;
        this.configuration.PinDraw = enabled;
    }

    public override void Draw()
    {
        if (this.loadError is not null || this.readingService is null)
        {
            ImGui.TextWrapped("Data failed to load.");
            ImGui.Separator();
            ImGui.TextWrapped(this.loadError ?? "Unknown load error.");
            return;
        }

        DrawControls();
        DrawReplaceSpreadModal();
        ImGui.Separator();
        DrawHeaderPanel();
        ImGui.Separator();
        DrawSpreadDisplay();
        ImGui.Separator();
        DrawOutputPanel();
        ImGui.Separator();
        ImGui.TextDisabled("Swami Sophie — Eorzean Mega Arcana (200)");
    }

    private void DrawControls()
    {
        var spreads = this.readingService!.Spreads;
        var outputModes = this.readingService.OutputModes;
        var spreadNames = spreads.Select(item => item.Name).ToArray();
        var outputNames = outputModes.Select(item => item.Name).ToArray();

        ImGui.SetNextItemWidth(220f);
        if (ImGui.Combo("Spread", ref this.spreadIndex, spreadNames, spreadNames.Length))
        {
            this.configuration.DefaultSpreadId = spreads[this.spreadIndex].Id;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(180f);
        if (ImGui.Combo("Output Mode", ref this.outputModeIndex, outputNames, outputNames.Length))
        {
            this.configuration.DefaultOutputModeId = outputModes[this.outputModeIndex].Id;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(180f);
        if (ImGui.Combo("Interpretation Bias", ref this.interpretationBiasIndex, InterpretationBiasOptions.All, InterpretationBiasOptions.All.Length))
        {
            var selectedBias = InterpretationBiasOptions.All[this.interpretationBiasIndex];
            this.configuration.InterpretationBias = selectedBias;
            this.readingService.InterpretationBias = selectedBias;
            this.configuration.Save();
        }

        ImGui.InputText("Question", ref this.question, 512);
        ImGui.Checkbox("Allow Repeats", ref this.allowRepeats);
        ImGui.SameLine();
        ImGui.Checkbox("Use Seed", ref this.useSeed);
        ImGui.SameLine();
        if (ImGui.Checkbox("Pin Draw", ref this.pinDraw))
        {
            this.configuration.PinDraw = this.pinDraw;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.InputInt("Seed Value", ref this.seedValue);

        var shouldDimPinnedButtons = this.pinDraw && this.lastDraw is not null;
        if (shouldDimPinnedButtons)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.6f);
        }

        if (ImGui.Button("Draw"))
        {
            RequestDraw(PendingDrawAction.Draw);
        }

        ImGui.SameLine();
        if (ImGui.Button("Redraw"))
        {
            RequestDraw(PendingDrawAction.Redraw);
        }

        if (shouldDimPinnedButtons)
        {
            ImGui.PopStyleVar();
        }

        if (this.pinDraw)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.86f, 0.63f, 0.22f, 1f), "Pinned");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Draws are pinned. Draw/Redraw will ask for confirmation before replacing the current spread.");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reinterpret"))
        {
            Reinterpret();
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy") && this.lastReading is not null)
        {
            ImGui.SetClipboardText(this.lastReading.Narrative);
            this.statusMessage = "Narrative copied to clipboard.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy Summary") && this.lastReading is not null)
        {
            ImGui.SetClipboardText(ReadingTextFormatter.BuildSummaryText(this.lastReading, this.readingService.Doctrine, this.readingService.InterpretationBias));
            this.statusMessage = "Summary copied to clipboard.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Export") && this.lastReading is not null)
        {
            ExportReading();
        }

        ImGui.SameLine();
        if (ImGui.Button("Open Deck Browser"))
        {
            this.toggleDeckBrowser();
        }

        ImGui.SameLine();
        if (ImGui.Button("Open History"))
        {
            this.toggleHistoryWindow();
        }

        ImGui.TextDisabled(this.statusMessage);
    }

    private void DrawReplaceSpreadModal()
    {
        if (!ImGui.BeginPopupModal(ReplaceSpreadPopupId, ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        ImGui.TextWrapped("Replace the current spread?");
        ImGui.Spacing();

        if (ImGui.Button("Confirm", new Vector2(120f, 0f)))
        {
            var action = this.pendingDrawAction;
            this.pendingDrawAction = PendingDrawAction.None;
            ImGui.CloseCurrentPopup();
            ExecuteDraw(action);
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120f, 0f)))
        {
            this.pendingDrawAction = PendingDrawAction.None;
            this.statusMessage = "Current spread kept.";
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void RequestDraw(PendingDrawAction action)
    {
        if (PinDrawBehavior.ShouldPrompt(this.pinDraw, this.lastDraw is not null, action))
        {
            this.pendingDrawAction = action;
            ImGui.OpenPopup(ReplaceSpreadPopupId);
            return;
        }

        ExecuteDraw(action);
    }

    private void ExecuteDraw(PendingDrawAction action)
    {
        switch (action)
        {
            case PendingDrawAction.Draw:
                DrawReading(useConfiguredSeed: this.useSeed);
                break;
            case PendingDrawAction.Redraw:
                DrawReading(useConfiguredSeed: this.useSeed, forceFreshSeed: !this.useSeed);
                break;
        }
    }

    private void DrawHeaderPanel()
    {
        if (this.lastReading is null)
        {
            ImGui.TextDisabled("No reading drawn yet.");
            return;
        }

        var header = this.lastReading.Header;
        ImGui.TextWrapped($"Scale: {header.Scale}");
        ImGui.TextWrapped($"Era State: {header.EraState}");
        ImGui.TextWrapped($"Dominant Element: {header.DominantElement ?? "None"}");
        ImGui.TextWrapped($"Escalation: {(header.Escalation ? "Yes" : "No")} ({JoinOrNone(header.EscalationReasons)})");
        ImGui.TextWrapped($"Moderation: {(header.Moderation ? "Yes" : "No")} ({JoinOrNone(header.ModerationReasons)})");
    }

    private void DrawSpreadDisplay()
    {
        if (this.lastReading is null)
        {
            ImGui.TextDisabled("Spread tiles will appear after a draw.");
            return;
        }

        var spread = this.lastDraw!.Spread;
        if (string.Equals(spread.Layout, "row", StringComparison.OrdinalIgnoreCase))
        {
            DrawRowLayout(this.lastReading.DrawnCards);
            return;
        }

        DrawGridLayout(this.lastReading.DrawnCards);
    }

    private void DrawRowLayout(IReadOnlyList<DrawnCard> drawnCards)
    {
        if (!ImGui.BeginTable("RowSpread", drawnCards.Count, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame))
        {
            return;
        }

        foreach (var card in drawnCards.OrderBy(item => item.Position.Index))
        {
            ImGui.TableNextColumn();
            DrawCardTile(card);
        }

        ImGui.EndTable();
    }

    private void DrawGridLayout(IReadOnlyList<DrawnCard> drawnCards)
    {
        if (!ImGui.BeginTable("GridSpread", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame))
        {
            return;
        }

        foreach (var card in drawnCards.OrderBy(item => item.Position.Index))
        {
            ImGui.TableNextColumn();
            DrawCardTile(card);
        }

        ImGui.EndTable();
    }

    private static void DrawCardTile(DrawnCard card)
    {
        ImGui.TextWrapped(card.Position.Name);
        ImGui.Separator();
        ImGui.TextWrapped(card.Card.Stratum);
        ImGui.TextWrapped(card.Card.Name);

        var detail = card.Card.Stratum switch
        {
            "Element" => $"{card.Card.Element} {card.Card.Rank}",
            "Court" => $"{card.Card.Element} {card.Card.Rank}",
            _ => card.Card.Element ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(detail))
        {
            ImGui.TextWrapped(detail);
        }
    }

    private void DrawOutputPanel()
    {
        if (this.lastReading is null)
        {
            ImGui.TextDisabled("Narrative output will appear here.");
            return;
        }

        ImGui.BeginChild("NarrativePanel", new Vector2(0, 0), true);
        ImGui.TextWrapped(this.lastReading.Narrative);
        ImGui.EndChild();
    }

    private void DrawReading(bool useConfiguredSeed, bool forceFreshSeed = false)
    {
        try
        {
            var spreads = this.readingService!.Spreads;
            var outputModes = this.readingService.OutputModes;
            var spread = spreads[this.spreadIndex];
            var outputMode = outputModes[this.outputModeIndex];
            var seed = useConfiguredSeed
                ? this.seedValue
                : forceFreshSeed
                    ? Environment.TickCount
                    : (int?)null;

            this.lastDraw = this.readingService.Draw(spread.Id, this.allowRepeats, seed);
            this.lastReading = this.readingService.Interpret(this.lastDraw, outputMode.Id, string.IsNullOrWhiteSpace(this.question) ? null : this.question.Trim());
            this.lastSeedUsed = seed.HasValue;

            this.configuration.DefaultSpreadId = spread.Id;
            this.configuration.DefaultOutputModeId = outputMode.Id;
            this.configuration.DefaultAllowRepeats = this.allowRepeats;
            this.configuration.DefaultUseSeed = this.useSeed;
            this.configuration.SeedValue = this.seedValue;
            this.configuration.Save();

            AppendHistoryEntry();

            this.statusMessage = this.lastReading.Seed is int actualSeed
                ? $"Reading drawn with seed {actualSeed}."
                : "Reading drawn.";
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Failed to draw reading.");
            this.statusMessage = ex.Message;
        }
    }

    private void Reinterpret()
    {
        if (this.lastDraw is null || this.readingService is null)
        {
            this.statusMessage = "No draw is available to reinterpret.";
            return;
        }

        try
        {
            var outputMode = this.readingService.OutputModes[this.outputModeIndex];
            this.lastReading = this.readingService.Interpret(this.lastDraw, outputMode.Id, string.IsNullOrWhiteSpace(this.question) ? null : this.question.Trim());
            AppendHistoryEntry();
            this.statusMessage = "Reading reinterpreted using the current output mode.";
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Failed to reinterpret reading.");
            this.statusMessage = ex.Message;
        }
    }

    private void AppendHistoryEntry()
    {
        if (this.lastDraw is null || this.lastReading is null)
        {
            return;
        }

        this.historyService.Add(new ReadingHistoryEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Question = string.IsNullOrWhiteSpace(this.question) ? null : this.question.Trim(),
            SpreadId = this.lastDraw.Spread.Id,
            SpreadName = this.lastDraw.Spread.Name,
            OutputModeId = this.lastReading.OutputModeId,
            SeedUsed = this.lastSeedUsed,
            Seed = this.lastDraw.Seed,
            AllowRepeats = this.allowRepeats,
            DrawnCardIds = this.lastDraw.DrawnCards.OrderBy(item => item.Position.Index).Select(item => item.Card.Id).ToArray(),
            Scale = this.lastReading.Header.Scale,
            EraState = this.lastReading.Header.EraState,
            CoreNarrativePreview = ReadingTextFormatter.BuildCoreNarrativePreview(this.lastReading.Narrative),
            ReadingResult = this.lastReading
        });
    }

    private void ExportReading()
    {
        if (this.lastReading is null)
        {
            return;
        }

        var directory = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "SwamiSophieExports");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"reading-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, ReadingTextFormatter.BuildExportText(this.lastReading));

        this.configuration.LastExportPath = path;
        this.configuration.Save();
        this.statusMessage = $"Exported reading to {path}.";
    }

    private int FindSpreadIndex(string spreadId)
    {
        var spreads = this.readingService!.Spreads;
        for (var index = 0; index < spreads.Count; index++)
        {
            if (string.Equals(spreads[index].Id, spreadId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private int FindOutputModeIndex(string outputModeId)
    {
        var modes = this.readingService!.OutputModes;
        for (var index = 0; index < modes.Count; index++)
        {
            if (string.Equals(modes[index].Id, outputModeId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private static int FindInterpretationBiasIndex(string interpretationBias)
    {
        for (var index = 0; index < InterpretationBiasOptions.All.Length; index++)
        {
            if (string.Equals(InterpretationBiasOptions.All[index], interpretationBias, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private static string JoinOrNone(IReadOnlyList<string> values) => values.Count == 0 ? "None" : string.Join("; ", values);
}

public interface IPluginLogAdapter
{
    void Error(Exception exception, string message);
}
