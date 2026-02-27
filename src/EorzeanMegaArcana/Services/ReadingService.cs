using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services;

public sealed class ReadingService
{
    private readonly DataBundle dataBundle;
    private readonly DeckService deckService;
    private readonly DrawService drawService;
    private readonly InterpretationEngine interpretationEngine;
    private string interpretationBias = InterpretationBiasOptions.Auto;

    public ReadingService(
        DataBundle dataBundle,
        DeckService deckService,
        DrawService drawService,
        InterpretationEngine interpretationEngine)
    {
        this.dataBundle = dataBundle;
        this.deckService = deckService;
        this.drawService = drawService;
        this.interpretationEngine = interpretationEngine;
    }

    public IReadOnlyList<SpreadDefinition> Spreads => this.dataBundle.SpreadsById.Values.OrderBy(item => item.CardCount).ToArray();

    public IReadOnlyList<OutputModeDefinition> OutputModes => this.dataBundle.OutputModes;

    public IReadOnlyList<Card> AllCards => this.dataBundle.AllCards;

    public string InterpretationBias
    {
        get => this.interpretationBias;
        set => this.interpretationBias = InterpretationBiasOptions.Normalize(value);
    }

    public DrawResult Draw(string spreadId, bool allowRepeats, int? seed = null)
    {
        if (!this.dataBundle.SpreadsById.TryGetValue(spreadId, out var spread))
        {
            throw new InvalidOperationException($"Unknown spread id: {spreadId}");
        }

        return this.drawService.Draw(spread, this.deckService.GetAllCards(), allowRepeats, seed);
    }

    public DrawResult RestoreDraw(string spreadId, IReadOnlyList<string> drawnCardIds, int? seed = null)
    {
        if (!this.dataBundle.SpreadsById.TryGetValue(spreadId, out var spread))
        {
            throw new InvalidOperationException($"Unknown spread id: {spreadId}");
        }

        if (drawnCardIds.Count != spread.Positions.Count)
        {
            throw new InvalidOperationException(
                $"Cannot restore spread '{spreadId}' with {drawnCardIds.Count} cards; expected {spread.Positions.Count}.");
        }

        var positions = spread.Positions.OrderBy(position => position.Index).ToArray();
        var drawnCards = positions
            .Select((position, index) => new DrawnCard
            {
                Position = position,
                Card = this.deckService.GetById(drawnCardIds[index])
            })
            .ToArray();

        return new DrawResult
        {
            Spread = spread,
            DrawnCards = drawnCards,
            Seed = seed
        };
    }

    public ReadingResult Interpret(DrawResult drawResult, string outputModeId, string? question = null)
    {
        var result = this.interpretationEngine.Interpret(drawResult, this.dataBundle.Doctrine, outputModeId, question);
        result.Narrative = ReadingFormatterFactory.Create(outputModeId, this.InterpretationBias).Format(result, this.dataBundle.Doctrine);
        return result;
    }

    public SpreadDefinition GetSpread(string spreadId) => this.dataBundle.SpreadsById[spreadId];

    public Doctrine Doctrine => this.dataBundle.Doctrine;
}
