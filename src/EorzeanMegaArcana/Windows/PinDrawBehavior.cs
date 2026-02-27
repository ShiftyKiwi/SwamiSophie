namespace EorzeanMegaArcana.Windows;

public enum PendingDrawAction
{
    None,
    Draw,
    Redraw
}

public static class PinDrawBehavior
{
    public static bool ShouldPrompt(bool pinDrawEnabled, bool hasCurrentDraw, PendingDrawAction action)
    {
        return pinDrawEnabled
            && hasCurrentDraw
            && action is PendingDrawAction.Draw or PendingDrawAction.Redraw;
    }
}
