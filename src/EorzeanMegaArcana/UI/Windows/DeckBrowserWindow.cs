using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EorzeanMegaArcana.Models;
using System.Numerics;
using System.Text;

namespace EorzeanMegaArcana.UI.Windows;

public sealed class DeckBrowserWindow : Window
{
    private static readonly string[] FlagOptions = ["Override", "Amplify", "Distort", "Moderate"];

    private readonly IReadOnlyList<Card> allCards;
    private readonly string[] stratumValues;
    private readonly string[] stratumLabels;
    private readonly string[] elementOptions;
    private readonly Random random = new();

    private string searchText = string.Empty;
    private int selectedStratumIndex;
    private int selectedElementIndex;
    private readonly Dictionary<string, bool> selectedFlags = FlagOptions.ToDictionary(flag => flag, _ => false, StringComparer.OrdinalIgnoreCase);
    private string? selectedCardId;

    public DeckBrowserWindow(IReadOnlyList<Card> allCards)
        : base("Deck Browser###SwamiSophieDeckBrowser")
    {
        this.allCards = allCards.OrderBy(card => card.Stratum).ThenBy(card => card.Name).ToArray();
        this.stratumValues = ["All", .. this.allCards.Select(card => card.Stratum).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)];
        this.stratumLabels = DeckBrowserSearch.BuildStratumLabels(this.allCards, this.stratumValues);
        this.elementOptions = ["All", .. this.allCards
            .Where(card => !string.IsNullOrWhiteSpace(card.Element))
            .Select(card => card.Element!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)];

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(920, 520),
            MaximumSize = new Vector2(1800, 1400)
        };
    }

    public override void Draw()
    {
        DrawFilters();
        ImGui.Separator();

        if (ImGui.BeginTable("DeckBrowserLayout", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Results", ImGuiTableColumnFlags.WidthStretch, 0.9f);
            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch, 1.1f);
            ImGui.TableNextColumn();
            DrawResults();
            ImGui.TableNextColumn();
            DrawDetails();
            ImGui.EndTable();
        }
    }

    private void DrawFilters()
    {
        ImGui.SetNextItemWidth(320f);
        ImGui.InputText("Search", ref this.searchText, 512);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(180f);
        ImGui.Combo("Stratum", ref this.selectedStratumIndex, this.stratumLabels, this.stratumLabels.Length);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(160f);
        ImGui.Combo("Element", ref this.selectedElementIndex, this.elementOptions, this.elementOptions.Length);

        ImGui.SameLine();
        if (ImGui.Button("Random Card"))
        {
            var filtered = GetFilteredCards();
            var selected = DeckBrowserSearch.SelectRandom(filtered, this.random);
            if (selected is not null)
            {
                this.selectedCardId = selected.Id;
            }
        }

        ImGui.TextUnformatted("Flags");
        foreach (var flag in FlagOptions)
        {
            var isSelected = this.selectedFlags[flag];
            if (ImGui.Checkbox(flag, ref isSelected))
            {
                this.selectedFlags[flag] = isSelected;
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawResults()
    {
        var filtered = GetFilteredCards();

        ImGui.BeginChild("DeckBrowserResults", new Vector2(0, 0), true);
        foreach (var card in filtered)
        {
            var isSelected = string.Equals(this.selectedCardId, card.Id, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable(BuildResultLabel(card), isSelected))
            {
                this.selectedCardId = card.Id;
            }
        }

        ImGui.EndChild();
    }

    private void DrawDetails()
    {
        var selectedCard = this.allCards.FirstOrDefault(card => string.Equals(card.Id, this.selectedCardId, StringComparison.OrdinalIgnoreCase));

        ImGui.BeginChild("DeckBrowserDetails", new Vector2(0, 0), true);
        if (selectedCard is null)
        {
            ImGui.TextDisabled("Select a card to inspect its details.");
            ImGui.EndChild();
            return;
        }

        ImGui.TextWrapped(selectedCard.Name);
        ImGui.TextDisabled(selectedCard.Id);
        ImGui.Separator();
        ImGui.TextWrapped($"Stratum: {selectedCard.Stratum}");
        ImGui.TextWrapped($"Element: {selectedCard.Element ?? "None"}");
        ImGui.TextWrapped($"Rank: {selectedCard.Rank ?? "None"}");
        ImGui.TextWrapped($"Polarity: {selectedCard.Polarity}");
        ImGui.TextWrapped($"Polarity Weight: {selectedCard.PolarityWeight}");
        ImGui.TextWrapped($"Flags: {(selectedCard.Flags.Count == 0 ? "None" : string.Join(", ", selectedCard.Flags))}");
        ImGui.Separator();
        DrawField("Core", selectedCard.Core);
        DrawField("Shadow", selectedCard.Shadow);
        DrawField("Astral Note", selectedCard.AstralNote);
        DrawField("Umbral Note", selectedCard.UmbralNote);

        if (ImGui.Button("Copy Card"))
        {
            ImGui.SetClipboardText(BuildCopyBlock(selectedCard));
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy ID"))
        {
            ImGui.SetClipboardText(selectedCard.Id);
        }

        ImGui.EndChild();
    }

    private IReadOnlyList<Card> GetFilteredCards()
    {
        return DeckBrowserSearch.Filter(
            this.allCards,
            this.searchText,
            this.stratumValues[this.selectedStratumIndex],
            this.elementOptions[this.selectedElementIndex],
            this.selectedFlags.Where(pair => pair.Value).Select(pair => pair.Key));
    }

    private static void DrawField(string label, string value)
    {
        ImGui.TextUnformatted(label);
        ImGui.TextWrapped(value);
        ImGui.Spacing();
    }

    private static string BuildResultLabel(Card card)
    {
        var builder = new StringBuilder();
        builder.Append('[').Append(card.Stratum).Append("] ").Append(card.Name);

        if (!string.IsNullOrWhiteSpace(card.Element))
        {
            builder.Append(" (").Append(card.Element);
            if (!string.IsNullOrWhiteSpace(card.Rank))
            {
                builder.Append(' ').Append(card.Rank);
            }

            builder.Append(')');
        }
        else if (!string.IsNullOrWhiteSpace(card.Rank))
        {
            builder.Append(" (").Append(card.Rank).Append(')');
        }

        if (card.Flags.Count > 0)
        {
            builder.Append("  ");
            builder.Append(string.Join(' ', card.Flags.Select(flag => $"[{flag}]")));
        }

        return builder.ToString();
    }

    private static string BuildCopyBlock(Card card)
    {
        var builder = new StringBuilder();
        builder.AppendLine(card.Name);
        builder.AppendLine($"Id: {card.Id}");
        builder.AppendLine($"Stratum: {card.Stratum}");
        builder.AppendLine($"Element: {card.Element ?? "None"}");
        builder.AppendLine($"Rank: {card.Rank ?? "None"}");
        builder.AppendLine($"Polarity: {card.Polarity}");
        builder.AppendLine($"PolarityWeight: {card.PolarityWeight}");
        builder.AppendLine($"Flags: {(card.Flags.Count == 0 ? "None" : string.Join(", ", card.Flags))}");
        builder.AppendLine();
        builder.AppendLine($"Core: {card.Core}");
        builder.AppendLine($"Shadow: {card.Shadow}");
        builder.AppendLine($"AstralNote: {card.AstralNote}");
        builder.AppendLine($"UmbralNote: {card.UmbralNote}");
        return builder.ToString().TrimEnd();
    }
}

public static class DeckBrowserSearch
{
    public static IReadOnlyList<Card> Filter(
        IReadOnlyList<Card> allCards,
        string? searchText,
        string? stratum,
        string? element,
        IEnumerable<string>? requiredFlags)
    {
        var normalizedSearch = searchText?.Trim();
        var requiredFlagSet = new HashSet<string>(requiredFlags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        return allCards
            .Where(card => MatchesStratum(card, stratum))
            .Where(card => MatchesElement(card, element))
            .Where(card => MatchesFlags(card, requiredFlagSet))
            .Where(card => MatchesSearch(card, normalizedSearch))
            .OrderBy(card => card.Stratum)
            .ThenBy(card => card.Name)
            .ToArray();
    }

    public static string[] BuildStratumLabels(IReadOnlyList<Card> allCards, IReadOnlyList<string> stratumValues)
    {
        return stratumValues
            .Select(stratum => string.Equals(stratum, "All", StringComparison.OrdinalIgnoreCase)
                ? $"All ({allCards.Count})"
                : $"{stratum} ({allCards.Count(card => string.Equals(card.Stratum, stratum, StringComparison.OrdinalIgnoreCase))})")
            .ToArray();
    }

    public static Card? SelectRandom(IReadOnlyList<Card> visibleCards, Random random)
    {
        if (visibleCards.Count == 0)
        {
            return null;
        }

        return visibleCards[random.Next(visibleCards.Count)];
    }

    private static bool MatchesStratum(Card card, string? stratum) =>
        string.IsNullOrWhiteSpace(stratum) ||
        string.Equals(stratum, "All", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(card.Stratum, stratum, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesElement(Card card, string? element) =>
        string.IsNullOrWhiteSpace(element) ||
        string.Equals(element, "All", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(card.Element, element, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesFlags(Card card, HashSet<string> requiredFlags) =>
        requiredFlags.Count == 0 || requiredFlags.All(flag => card.Flags.Contains(flag, StringComparer.OrdinalIgnoreCase));

    private static bool MatchesSearch(Card card, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return new[]
            {
                card.Name,
                card.Id,
                card.Core,
                card.Shadow,
                card.AstralNote,
                card.UmbralNote
            }
            .Any(value => value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }
}
