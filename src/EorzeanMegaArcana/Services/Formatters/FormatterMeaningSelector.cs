using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services.Formatters;

internal static class FormatterMeaningSelector
{
    public static string SelectMeaning(ReadingResult result, DrawnCard drawnCard, string interpretationBias)
    {
        var normalizedBias = InterpretationBiasOptions.Normalize(interpretationBias);
        var useShadow = normalizedBias switch
        {
            InterpretationBiasOptions.PreferShadow => true,
            InterpretationBiasOptions.PreferCore => false,
            InterpretationBiasOptions.StrictAuto => HasContextualPolarityMismatch(result, drawnCard.Card),
            _ => HasContextualPolarityMismatch(result, drawnCard.Card) || HasCalamityBias(result, drawnCard.Card)
        };

        var primary = useShadow ? drawnCard.Card.Shadow : drawnCard.Card.Core;
        var fallback = useShadow ? drawnCard.Card.Core : drawnCard.Card.Shadow;
        return FirstNonEmpty(primary, fallback, drawnCard.Card.Name);
    }

    public static string SelectNote(ReadingResult result, DrawnCard drawnCard)
    {
        return result.Header.EraState switch
        {
            "Astral" => FirstNonEmpty(drawnCard.Card.AstralNote, drawnCard.Card.UmbralNote, drawnCard.Card.Core, drawnCard.Card.Shadow),
            "Umbral" => FirstNonEmpty(drawnCard.Card.UmbralNote, drawnCard.Card.AstralNote, drawnCard.Card.Shadow, drawnCard.Card.Core),
            _ => string.Join(" ", new[] { drawnCard.Card.AstralNote, drawnCard.Card.UmbralNote }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim()
                 is { Length: > 0 } combined
                    ? combined
                    : FirstNonEmpty(drawnCard.Card.Core, drawnCard.Card.Shadow, drawnCard.Card.Name)
        };
    }

    private static bool HasContextualPolarityMismatch(ReadingResult result, Card card)
    {
        return (string.Equals(result.Header.EraState, "Umbral", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(card.Polarity, "Astral", StringComparison.OrdinalIgnoreCase))
            || (string.Equals(result.Header.EraState, "Astral", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(card.Polarity, "Umbral", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCalamityBias(ReadingResult result, Card card)
    {
        return result.Diagnostics.CountsByStratum.TryGetValue("Calamity", out var calamityCount)
            && calamityCount > 0
            && !string.Equals(card.Stratum, "Divine", StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        return candidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
