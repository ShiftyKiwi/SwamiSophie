namespace EorzeanMegaArcana.Models;

public static class InterpretationBiasOptions
{
    public const string Auto = "Auto";
    public const string PreferCore = "PreferCore";
    public const string PreferShadow = "PreferShadow";
    public const string StrictAuto = "StrictAuto";

    public static readonly string[] All = [Auto, PreferCore, PreferShadow, StrictAuto];

    public static string Normalize(string? value)
    {
        return All.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? All.First(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
            : Auto;
    }
}
