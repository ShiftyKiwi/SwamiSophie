using Dalamud.Configuration;
using EorzeanMegaArcana.Models;
using System;

namespace EorzeanMegaArcana;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string DefaultSpreadId { get; set; } = "aether-pulse";

    public string DefaultOutputModeId { get; set; } = "concise";

    public string InterpretationBias { get; set; } = InterpretationBiasOptions.Auto;

    public bool DefaultAllowRepeats { get; set; }

    public bool DefaultUseSeed { get; set; }

    public bool PinDraw { get; set; }

    public int SeedValue { get; set; } = 777;

    public bool PersistHistory { get; set; }

    public List<PersistedReadingHistoryEntry> PersistedHistory { get; set; } = new();

    public string? LastExportPath { get; set; }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
