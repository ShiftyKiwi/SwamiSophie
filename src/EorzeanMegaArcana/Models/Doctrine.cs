using System.Text.Json.Serialization;

namespace EorzeanMegaArcana.Models;

public sealed class Doctrine
{
    [JsonPropertyName("authorityOrder")]
    public IReadOnlyList<string> AuthorityOrder { get; set; } = Array.Empty<string>();

    [JsonPropertyName("scaleRules")]
    public ScaleRules ScaleRules { get; set; } = new();

    [JsonPropertyName("polarity")]
    public PolarityRules Polarity { get; set; } = new();
}

public sealed class ScaleRules
{
    [JsonPropertyName("minor")]
    public ScaleRuleSet Minor { get; set; } = new();

    [JsonPropertyName("major")]
    public ScaleRuleSet Major { get; set; } = new();

    [JsonPropertyName("era")]
    public ScaleRuleSet Era { get; set; } = new();
}

public sealed class ScaleRuleSet
{
    [JsonPropertyName("conditions")]
    public IReadOnlyList<ScaleCondition> Conditions { get; set; } = Array.Empty<ScaleCondition>();

    [JsonPropertyName("conditionsAny")]
    public IReadOnlyList<ScaleCondition> ConditionsAny { get; set; } = Array.Empty<ScaleCondition>();
}

public sealed class ScaleCondition
{
    [JsonPropertyName("axisIsStratum")]
    public string? AxisIsStratum { get; set; }

    [JsonPropertyName("axisNotStratum")]
    public string? AxisNotStratum { get; set; }

    [JsonPropertyName("stratumPresent")]
    public string? StratumPresent { get; set; }

    [JsonPropertyName("stratumAbsent")]
    public string? StratumAbsent { get; set; }

    [JsonPropertyName("minCountByStratum")]
    public Dictionary<string, int>? MinCountByStratum { get; set; }

    [JsonPropertyName("maxCountByStratum")]
    public Dictionary<string, int>? MaxCountByStratum { get; set; }
}

public sealed class PolarityRules
{
    [JsonPropertyName("astralThreshold")]
    public int AstralThreshold { get; set; }

    [JsonPropertyName("umbralThreshold")]
    public int UmbralThreshold { get; set; }

    [JsonPropertyName("divineBalanceCardIds")]
    public IReadOnlyList<string> DivineBalanceCardIds { get; set; } = Array.Empty<string>();

    [JsonPropertyName("stabilizeByOneTierIfBalancePresent")]
    public bool StabilizeByOneTierIfBalancePresent { get; set; }
}
