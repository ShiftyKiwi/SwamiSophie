using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace EorzeanMegaArcana.UI.Windows;

public sealed class GuideWindow : Window
{
    public GuideWindow()
        : base("Guides###SwamiSophieGuideWindow")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 520),
            MaximumSize = new Vector2(1400, 1100)
        };
    }

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("SwamiSophieGuides"))
        {
            return;
        }

        if (ImGui.BeginTabItem("Usage"))
        {
            DrawUsageTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Interpretation"))
        {
            DrawInterpretationTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private static void DrawUsageTab()
    {
        ImGui.BeginChild("GuideUsage", new Vector2(0, 0), false);

        DrawSection(
            "Opening",
            "Use /swami or /ema to open the main window.");

        DrawSection(
            "Workflow",
            "Choose a spread, optionally enter a question, choose output mode and interpretation bias, then draw.");
        ImGui.BulletText("Aether Pulse: focused 3-card reading.");
        ImGui.BulletText("Convergence of the Star: layered 9-card reading.");
        ImGui.BulletText("Concise: quick reading.");
        ImGui.BulletText("Layered: doctrinal sections.");
        ImGui.BulletText("Scholarly: layered output plus diagnostics.");

        DrawSection(
            "Header",
            "Read the header before the narrative.");
        ImGui.BulletText("Scale: Minor, Major, or Era.");
        ImGui.BulletText("Era State: Astral, Umbral, or Transitional.");
        ImGui.BulletText("Dominant Element: top Element and Court influence, if any.");
        ImGui.BulletText("Escalation and Moderation: pressure versus containment.");

        DrawSection(
            "Utilities",
            "Use the surrounding tools to study and retain readings.");
        ImGui.BulletText("Deck Browser: search, filter, inspect, and copy cards.");
        ImGui.BulletText("History: reopen, reinterpret, copy, and export prior readings.");
        ImGui.BulletText("Copy Summary: compact structured output.");
        ImGui.BulletText("Pin Draw: asks for confirmation before replacing a spread.");

        ImGui.EndChild();
    }

    private static void DrawInterpretationTab()
    {
        ImGui.BeginChild("GuideInterpretation", new Vector2(0, 0), false);

        DrawSection(
            "Principle",
            "Swami Sophie is diagnostic rather than predictive. It suggests patterns of influence; it does not promise outcomes.");

        DrawSection(
            "Read Order",
            "Interpret the system in order.");
        ImGui.BulletText("1. Scale");
        ImGui.BulletText("2. Era State");
        ImGui.BulletText("3. Highest Authority");
        ImGui.BulletText("4. Escalation versus Moderation");
        ImGui.BulletText("5. Dominant Element");
        ImGui.BulletText("6. Position");

        DrawSection(
            "Authority",
            "Higher strata frame the lower ones.");
        ImGui.BulletText("Calamity");
        ImGui.BulletText("Crystal");
        ImGui.BulletText("Divine");
        ImGui.BulletText("Primal");
        ImGui.BulletText("Shard");
        ImGui.BulletText("Persona");
        ImGui.BulletText("Court");
        ImGui.BulletText("Element");

        DrawSection(
            "Element Expression",
            "Dominant element describes how influence expresses itself.");
        ImGui.BulletText("Fire: action");
        ImGui.BulletText("Ice: control");
        ImGui.BulletText("Wind: thought or movement");
        ImGui.BulletText("Earth: structure");
        ImGui.BulletText("Lightning: decision or revelation");
        ImGui.BulletText("Water: relationship");

        DrawSection(
            "Position",
            "Position gives the reading shape.");
        ImGui.BulletText("Aether Pulse: Pressure, Axis, Direction.");
        ImGui.BulletText("Convergence of the Star: First Row, Second Row, Third Row.");

        ImGui.EndChild();
    }

    private static void DrawSection(string heading, string body)
    {
        ImGui.TextUnformatted(heading);
        ImGui.TextWrapped(body);
        ImGui.Spacing();
    }
}
