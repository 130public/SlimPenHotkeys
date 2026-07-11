using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SlimPenHotkeys.Core;
using Windows.UI.ViewManagement;

namespace SlimPenHotkeys;

/// <summary>
/// Application root: owns the remap engine, tray icon, and the (hideable) settings
/// window. Behaves as a tray utility — closing the window hides it rather than
/// exiting, and launching at login starts hidden.
/// </summary>
public partial class App : Application
{
    public static App Instance { get; private set; } = null!;

    internal RemapEngine Engine { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = null!;

    private MainWindow? _window;
    private TrayIcon? _tray;
    private UISettings? _uiSettings;   // kept alive; fires ColorValuesChanged on theme change
    private DispatcherQueue _dispatcher = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Instance = this;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        Settings = AppSettings.Load();

        Engine = new RemapEngine();
        Engine.ApplyConfig(Settings);
        Engine.ShowWindowRequested += () => _dispatcher.TryEnqueue(ShowMainWindow);
        Engine.Start();

        _tray = new TrayIcon();
        _tray.ShowRequested += () => _dispatcher.TryEnqueue(ShowMainWindow);
        _tray.ToggleRequested += () => _dispatcher.TryEnqueue(ToggleAll);
        _tray.ExitRequested += () => _dispatcher.TryEnqueue(ExitApp);
        _tray.Create(TrayTooltip());

        _window = new MainWindow();
        _window.AppWindow.Closing += OnWindowClosing;

        // Refresh the theme-specific icons (title bar, taskbar, tray) whenever the
        // Windows light/dark theme changes. ColorValuesChanged fires on a thread-
        // pool thread, so marshal the update onto the UI thread.
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += (_, _) => _dispatcher.TryEnqueue(ApplyThemeIcons);

        // Second-instance "show" signal (see Program.cs single-instance guard).
        Program.ShowSignal += () => _dispatcher.TryEnqueue(ShowMainWindow);

        if (LaunchedAtStartup())
            _window.AppWindow.Hide();
        else
            ShowMainWindow();
    }

    private static bool LaunchedAtStartup()
    {
        try
        {
            var kind = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()
                .GetActivatedEventArgs().Kind;
            return kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.StartupTask;
        }
        catch
        {
            return false;
        }
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs e)
    {
        // Hide to tray instead of terminating.
        e.Cancel = true;
        HideMainWindow();
    }

    public void ShowMainWindow()
    {
        _window ??= new MainWindow();
        _window.AppWindow.Show();
        _window.Page?.ReloadFromSettings();
        _window.Activate();
        try { _window.AppWindow.MoveInZOrderAtTop(); } catch { /* best effort */ }
    }

    public void HideMainWindow() => _window?.AppWindow.Hide();

    private void ApplyThemeIcons()
    {
        _window?.ApplyThemeIcons();
        _tray?.UpdateIcon();
    }

    public void UpdateTrayTooltip() => _tray?.SetTooltip(TrayTooltip());

    private string TrayTooltip() => "SlimPen Hotkeys - " + (Settings.AnyEnabled ? "ON" : "OFF");

    private void ToggleAll()
    {
        bool newState = !Settings.AnyEnabled;
        Settings.Single.Enabled = newState;
        Settings.Double.Enabled = newState;
        Settings.Long.Enabled = newState;
        Settings.Save();
        Engine.ApplyConfig(Settings);
        UpdateTrayTooltip();
        _window?.Page?.ReloadFromSettings();
    }

    private void ExitApp()
    {
        try { _tray?.Dispose(); } catch { }
        try { Engine.Stop(); } catch { }
        Exit();
    }
}
