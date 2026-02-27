using EorzeanMegaArcana.Models;
using System.Text.Json;

namespace EorzeanMegaArcana.Services;

public sealed class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly string dataRoot;

    public DataLoader(string dataRoot)
    {
        this.dataRoot = dataRoot;
    }

    public DataBundle Load()
    {
        var rulesRoot = Path.Combine(this.dataRoot, "rules");
        var strataRoot = Path.Combine(this.dataRoot, "strata");

        var doctrine = LoadRequired<Doctrine>(Path.Combine(rulesRoot, "doctrine.json"));
        var spreads = LoadRequired<List<SpreadDefinition>>(Path.Combine(rulesRoot, "spreads.json"));
        var outputModes = LoadRequired<List<OutputModeDefinition>>(Path.Combine(rulesRoot, "output_modes.json"));

        var cards = Directory.EnumerateFiles(strataRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => LoadRequired<List<Card>>(path))
            .ToList();

        Validate(doctrine, spreads, outputModes, cards);

        return new DataBundle
        {
            AllCards = cards,
            SpreadsById = spreads.ToDictionary(spread => spread.Id, StringComparer.OrdinalIgnoreCase),
            OutputModes = outputModes,
            Doctrine = doctrine
        };
    }

    private static T LoadRequired<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required data file missing: {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize data file: {path}");
    }

    private static void Validate(
        Doctrine doctrine,
        IReadOnlyList<SpreadDefinition> spreads,
        IReadOnlyList<OutputModeDefinition> outputModes,
        IReadOnlyList<Card> cards)
    {
        if (doctrine.AuthorityOrder.Count == 0)
        {
            throw new InvalidOperationException("doctrine.authorityOrder is required.");
        }

        if (spreads.Count == 0)
        {
            throw new InvalidOperationException("At least one spread is required.");
        }

        foreach (var spread in spreads)
        {
            if (string.IsNullOrWhiteSpace(spread.Id) || string.IsNullOrWhiteSpace(spread.Name))
            {
                throw new InvalidOperationException("Each spread requires id and name.");
            }

            if (spread.CardCount <= 0 || spread.Positions.Count != spread.CardCount)
            {
                throw new InvalidOperationException($"Spread {spread.Id} has invalid cardCount/positions.");
            }

            if (spread.AxisIndex < 1 || spread.AxisIndex > spread.CardCount)
            {
                throw new InvalidOperationException($"Spread {spread.Id} has invalid axisIndex.");
            }
        }

        if (outputModes.Count == 0)
        {
            throw new InvalidOperationException("At least one output mode is required.");
        }

        if (cards.Count == 0)
        {
            throw new InvalidOperationException("At least one card is required.");
        }

        foreach (var card in cards)
        {
            if (string.IsNullOrWhiteSpace(card.Id) ||
                string.IsNullOrWhiteSpace(card.Name) ||
                string.IsNullOrWhiteSpace(card.Stratum) ||
                string.IsNullOrWhiteSpace(card.Polarity) ||
                string.IsNullOrWhiteSpace(card.Core) ||
                string.IsNullOrWhiteSpace(card.Shadow) ||
                string.IsNullOrWhiteSpace(card.AstralNote) ||
                string.IsNullOrWhiteSpace(card.UmbralNote))
            {
                throw new InvalidOperationException($"Card {card.Id} is missing required fields.");
            }
        }

        var duplicateCardIds = cards
            .GroupBy(card => card.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateCardIds.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate card ids found: {string.Join(", ", duplicateCardIds)}");
        }
    }
}
