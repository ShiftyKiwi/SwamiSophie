using EorzeanMegaArcana.Windows;
using Xunit;

namespace EorzeanMegaArcana.Tests;

public sealed class PinDrawTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void ConfigurationDefinesPinDrawSetting()
    {
        var configurationSource = File.ReadAllText(Path.Combine(RepoRoot, "src", "EorzeanMegaArcana", "Configuration.cs"));
        Assert.Contains("public bool PinDraw { get; set; }", configurationSource);
    }

    [Theory]
    [InlineData(false, false, PendingDrawAction.Draw, false)]
    [InlineData(true, false, PendingDrawAction.Draw, false)]
    [InlineData(true, true, PendingDrawAction.Draw, true)]
    [InlineData(true, true, PendingDrawAction.Redraw, true)]
    [InlineData(true, true, PendingDrawAction.None, false)]
    [InlineData(false, true, PendingDrawAction.Redraw, false)]
    public void PinDrawPromptBehaviorMatchesExpectedState(bool pinDrawEnabled, bool hasCurrentDraw, PendingDrawAction action, bool expected)
    {
        Assert.Equal(expected, PinDrawBehavior.ShouldPrompt(pinDrawEnabled, hasCurrentDraw, action));
    }
}
