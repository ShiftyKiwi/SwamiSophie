using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EorzeanMegaArcana.Services;
using EorzeanMegaArcana.UI.Windows;
using EorzeanMegaArcana.Windows;

namespace EorzeanMegaArcana;

public sealed class Plugin : IDalamudPlugin, IPluginLogAdapter
{
    private const string MainCommandName = "/swami";
    private const string AliasCommandName = "/ema";

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog StaticLog { get; private set; } = null!;

    private readonly MainWindow mainWindow;
    private readonly DeckBrowserWindow? deckBrowserWindow;
    private readonly HistoryWindow historyWindow;
    private readonly GuideWindow guideWindow;
    private readonly ReadingHistoryService historyService;
    private readonly WindowSystem windowSystem = new("Swami Sophie");

    public Plugin()
    {
        Configuration configuration;
        try
        {
            configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
        catch (Exception ex)
        {
            StaticLog.Warning(ex, "Failed to load configuration. Using defaults.");
            configuration = new Configuration();
        }

        this.historyService = new ReadingHistoryService();
        ReadingService? readingService = null;
        string? loadError = null;

        try
        {
            var assemblyDirectory = PluginInterface.AssemblyLocation.Directory?.FullName
                ?? throw new InvalidOperationException("Unable to determine plugin assembly directory.");
            var dataRoot = Path.Combine(assemblyDirectory, "data");
            var dataBundle = new DataLoader(dataRoot).Load();
            readingService = new ReadingService(
                dataBundle,
                new DeckService(dataBundle.AllCards),
                new DrawService(),
                new InterpretationEngine());
            this.deckBrowserWindow = new DeckBrowserWindow(readingService.AllCards);
        }
        catch (Exception ex)
        {
            loadError = ex.Message;
            StaticLog.Error(ex, "Failed to load plugin data.");
        }

        if (configuration.PersistHistory)
        {
            try
            {
                this.historyService.LoadPersistedEntries(configuration.PersistedHistory);
            }
            catch (Exception ex)
            {
                this.historyService.Clear();
                configuration.PersistedHistory = new();
                StaticLog.Warning(ex, "Failed to load persisted reading history. History was cleared.");
            }
        }
        else
        {
            configuration.PersistedHistory = new();
        }

        this.Configuration = configuration;
        this.Log = StaticLog;
        this.mainWindow = new MainWindow(configuration, this, readingService, this.historyService, loadError, ToggleDeckBrowser, ToggleHistoryWindow, ToggleGuideWindow);
        this.historyWindow = new HistoryWindow(this.historyService, readingService, this.mainWindow.RestoreHistoryEntry);
        this.guideWindow = new GuideWindow();

        this.windowSystem.AddWindow(this.mainWindow);
        if (this.deckBrowserWindow is not null)
        {
            this.windowSystem.AddWindow(this.deckBrowserWindow);
        }

        this.windowSystem.AddWindow(this.historyWindow);
        this.windowSystem.AddWindow(this.guideWindow);

        var commandInfo = new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Swami Sophie."
        };

        CommandManager.AddHandler(MainCommandName, commandInfo);
        CommandManager.AddHandler(AliasCommandName, commandInfo);
        PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
    }

    public Configuration Configuration { get; }

    public IPluginLog Log { get; }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        this.windowSystem.RemoveAllWindows();
        this.mainWindow.Dispose();

        try
        {
            this.Configuration.PersistedHistory = this.Configuration.PersistHistory
                ? this.historyService.ToPersistedEntries().ToList()
                : new();
            this.Configuration.Save();
        }
        catch (Exception ex)
        {
            this.Log.Warning(ex, "Failed to save configuration during shutdown.");
        }

        CommandManager.RemoveHandler(MainCommandName);
        CommandManager.RemoveHandler(AliasCommandName);
    }

    public void ToggleMainUi()
    {
        this.mainWindow.Toggle();
    }

    public void ToggleDeckBrowser()
    {
        this.deckBrowserWindow?.Toggle();
    }

    public void ToggleHistoryWindow()
    {
        this.historyWindow.Toggle();
    }

    public void ToggleGuideWindow()
    {
        this.guideWindow.Toggle();
    }

    public void Error(Exception exception, string message)
    {
        this.Log.Error(exception, message);
    }

    private void OnCommand(string command, string arguments)
    {
        ToggleMainUi();
    }
}
