using EorzeanMegaArcana.Models;
using System.Text.Json;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class DataValidationTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static readonly string RulesRoot = Path.Combine(RepoRoot, "data", "rules");
    private static readonly string StrataRoot = Path.Combine(RepoRoot, "data", "strata");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly HashSet<string> ValidStrata = new(StringComparer.OrdinalIgnoreCase)
    {
        "Crystal",
        "Primal",
        "Divine",
        "Calamity",
        "Shard",
        "Persona",
        "Court",
        "Element"
    };

    private static readonly HashSet<string> ValidCourtRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        "Initiate",
        "Vanguard",
        "Conduit",
        "Sovereign"
    };

    private static readonly HashSet<string> KnownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Override",
        "Amplify",
        "Distort",
        "Moderate"
    };

    [Fact]
    public void EveryRulesAndStrataJsonFileParsesSuccessfully()
    {
        var errors = new List<string>();

        foreach (var file in EnumerateJsonFiles(RulesRoot).Concat(EnumerateJsonFiles(StrataRoot)))
        {
            try
            {
                using var _ = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (Exception ex)
            {
                errors.Add($"{Relative(file)}: failed to parse JSON - {ex.Message}");
            }
        }

        AssertNoErrors(errors);
    }

    [Fact]
    public void EveryCardInStrataFilesMatchesDeckConstraints()
    {
        var errors = new List<string>();
        var seenIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateJsonFiles(StrataRoot))
        {
            List<Card>? cards;
            try
            {
                cards = JsonSerializer.Deserialize<List<Card>>(File.ReadAllText(file), JsonOptions);
            }
            catch (Exception ex)
            {
                errors.Add($"{Relative(file)}: failed to deserialize cards - {ex.Message}");
                continue;
            }

            if (cards is null)
            {
                errors.Add($"{Relative(file)}: deserialized to null.");
                continue;
            }

            foreach (var card in cards)
            {
                var cardRef = $"{Relative(file)} [{CardId(card)}]";

                if (string.IsNullOrWhiteSpace(card.Id))
                {
                    errors.Add($"{cardRef}: missing required field 'id'.");
                }

                if (string.IsNullOrWhiteSpace(card.Name))
                {
                    errors.Add($"{cardRef}: missing required field 'name'.");
                }

                if (string.IsNullOrWhiteSpace(card.Stratum))
                {
                    errors.Add($"{cardRef}: missing required field 'stratum'.");
                }

                if (string.IsNullOrWhiteSpace(card.Polarity))
                {
                    errors.Add($"{cardRef}: missing required field 'polarity'.");
                }

                if (string.IsNullOrWhiteSpace(card.Core))
                {
                    errors.Add($"{cardRef}: missing required field 'core'.");
                }

                if (string.IsNullOrWhiteSpace(card.Shadow))
                {
                    errors.Add($"{cardRef}: missing required field 'shadow'.");
                }

                if (string.IsNullOrWhiteSpace(card.AstralNote))
                {
                    errors.Add($"{cardRef}: missing required field 'astralNote'.");
                }

                if (string.IsNullOrWhiteSpace(card.UmbralNote))
                {
                    errors.Add($"{cardRef}: missing required field 'umbralNote'.");
                }

                if (!string.IsNullOrWhiteSpace(card.Id))
                {
                    if (seenIds.TryGetValue(card.Id, out var firstFile))
                    {
                        errors.Add($"{cardRef}: duplicate id '{card.Id}' already defined in {firstFile}.");
                    }
                    else
                    {
                        seenIds[card.Id] = Relative(file);
                    }
                }

                if (string.IsNullOrWhiteSpace(card.Stratum) || !ValidStrata.Contains(card.Stratum))
                {
                    errors.Add($"{cardRef}: invalid stratum '{card.Stratum ?? "null"}'.");
                    continue;
                }

                if (card.PolarityWeight < -2 || card.PolarityWeight > 2)
                {
                    errors.Add($"{cardRef}: polarityWeight {card.PolarityWeight} is outside [-2..2].");
                }

                if (string.Equals(card.Stratum, "Element", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(card.Element))
                    {
                        errors.Add($"{cardRef}: Element cards require a non-null element.");
                    }

                    if (string.IsNullOrWhiteSpace(card.Rank) || !Enumerable.Range(1, 10).Select(value => value.ToString()).Contains(card.Rank, StringComparer.Ordinal))
                    {
                        errors.Add($"{cardRef}: Element cards require rank in '1'..'10', found '{card.Rank ?? "null"}'.");
                    }
                }

                if (string.Equals(card.Stratum, "Court", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(card.Element))
                    {
                        errors.Add($"{cardRef}: Court cards require a non-null element.");
                    }

                    if (string.IsNullOrWhiteSpace(card.Rank) || !ValidCourtRanks.Contains(card.Rank))
                    {
                        errors.Add($"{cardRef}: Court cards require rank Initiate/Vanguard/Conduit/Sovereign, found '{card.Rank ?? "null"}'.");
                    }
                }

                foreach (var flag in card.Flags)
                {
                    if (!KnownFlags.Contains(flag))
                    {
                        errors.Add($"{cardRef}: unknown flag '{flag}'.");
                    }
                }
            }
        }

        AssertNoErrors(errors);
    }

    private static IEnumerable<string> EnumerateJsonFiles(string root) =>
        Directory.EnumerateFiles(root, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

    private static string Relative(string fullPath) => Path.GetRelativePath(RepoRoot, fullPath);

    private static string CardId(Card card) => string.IsNullOrWhiteSpace(card.Id) ? "<missing-id>" : card.Id;

    private static void AssertNoErrors(List<string> errors)
    {
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
