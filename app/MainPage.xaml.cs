using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SlimPenHotkeys.Core;
using Windows.UI;

namespace SlimPenHotkeys;

/// <summary>
/// Settings UI for the pen remapper. Reads/writes the shared <see cref="AppSettings"/>,
/// drives the <see cref="RemapEngine"/>, and hosts the Learn capture flow.
/// </summary>
public sealed partial class MainPage : Page
{
    private bool _loading;

    private readonly DispatcherTimer _penResetTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly DispatcherTimer _hotkeyResetTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private static readonly Brush ActiveBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x0F, 0x7B, 0x0F)); // green
    private static readonly Brush FireBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x5A, 0x9E));   // blue

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _penResetTimer.Tick += (_, _) => { _penResetTimer.Stop(); ResetPenIndicator(); };
        _hotkeyResetTimer.Tick += (_, _) => { _hotkeyResetTimer.Stop(); ResetHotkeyIndicator(); };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadFromSettings();
        Engine.TriggerPressed += OnTriggerPressed;
        Engine.HotkeyFired += OnHotkeyFired;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Engine.TriggerPressed -= OnTriggerPressed;
        Engine.HotkeyFired -= OnHotkeyFired;
    }

    private RemapEngine Engine => App.Instance.Engine;
    private AppSettings Settings => App.Instance.Settings;

    // ---- Navigation ------------------------------------------------------
    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string tag = (args.SelectedItem as NavigationViewItem)?.Tag as string ?? "single";
        SinglePanel.Visibility = tag == "single" ? Visibility.Visible : Visibility.Collapsed;
        DoublePanel.Visibility = tag == "double" ? Visibility.Visible : Visibility.Collapsed;
        LongPanel.Visibility = tag == "long" ? Visibility.Visible : Visibility.Collapsed;
        TestPanel.Visibility = tag == "test" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Reload all controls from the shared settings (e.g. after a tray toggle).</summary>
    public void ReloadFromSettings() => LoadFromSettings();

    private void LoadFromSettings()
    {
        _loading = true;
        var s = Settings;

        SingleTrigger.Text = s.Single.Trigger;
        SingleHotkey.Text = s.Single.Hotkey;
        SingleEnabled.IsChecked = s.Single.Enabled;

        DoubleTrigger.Text = s.Double.Trigger;
        DoubleHotkey.Text = s.Double.Hotkey;
        DoubleEnabled.IsChecked = s.Double.Enabled;

        LongTrigger.Text = s.Long.Trigger;
        LongHotkey.Text = s.Long.Hotkey;
        LongEnabled.IsChecked = s.Long.Enabled;

        HoldMs.Value = s.HoldMs;

        UpdateToggleButton();
        _loading = false;

        _ = RefreshLaunchAtLoginAsync();
    }

    private async Task RefreshLaunchAtLoginAsync()
    {
        bool enabled = await StartupManager.IsEnabledAsync();
        _loading = true;
        LaunchAtLogin.IsChecked = enabled;
        _loading = false;
    }

    private void ReadControlsIntoSettings()
    {
        var s = Settings;
        s.Single.Trigger = SingleTrigger.Text.Trim();
        s.Single.Hotkey = SingleHotkey.Text.Trim();
        s.Single.Enabled = SingleEnabled.IsChecked == true;

        s.Double.Trigger = DoubleTrigger.Text.Trim();
        s.Double.Hotkey = DoubleHotkey.Text.Trim();
        s.Double.Enabled = DoubleEnabled.IsChecked == true;

        s.Long.Trigger = LongTrigger.Text.Trim();
        s.Long.Hotkey = LongHotkey.Text.Trim();
        s.Long.Enabled = LongEnabled.IsChecked == true;

        int hold = double.IsNaN(HoldMs.Value) ? 120 : (int)HoldMs.Value;
        s.HoldMs = hold < 1 ? 120 : hold;
    }

    private void UpdateToggleButton()
    {
        bool anyOn = SingleEnabled.IsChecked == true
                     || DoubleEnabled.IsChecked == true
                     || LongEnabled.IsChecked == true;
        ToggleButton.Content = anyOn
            ? "✅  ACTIVE  —  click to disable all"
            : "⛔  ALL DISABLED  —  click to enable";
    }

    private void SetStatus(string msg) => StatusText.Text = msg;

    // ---- Save / Defaults / Hide -----------------------------------------
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ReadControlsIntoSettings();
        Settings.Save();
        Engine.ApplyConfig(Settings);
        UpdateToggleButton();
        App.Instance.UpdateTrayTooltip();
        SetStatus("Saved.");
    }

    private void DefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var d = AppSettings.Defaults();
        Settings.Single = d.Single;
        Settings.Double = d.Double;
        Settings.Long = d.Long;
        Settings.HoldMs = d.HoldMs;
        LoadFromSettings();
        SetStatus("Defaults restored. Click Save to persist.");
    }

    private void HideButton_Click(object sender, RoutedEventArgs e) => App.Instance.HideMainWindow();

    // ---- Master toggle ---------------------------------------------------
    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        bool anyOn = SingleEnabled.IsChecked == true
                     || DoubleEnabled.IsChecked == true
                     || LongEnabled.IsChecked == true;
        bool newState = !anyOn;
        SingleEnabled.IsChecked = newState;
        DoubleEnabled.IsChecked = newState;
        LongEnabled.IsChecked = newState;

        ReadControlsIntoSettings();
        Settings.Save();
        Engine.ApplyConfig(Settings);
        UpdateToggleButton();
        App.Instance.UpdateTrayTooltip();
        SetStatus(newState ? "All mappings enabled." : "All mappings disabled.");
    }

    // ---- Launch at login -------------------------------------------------
    private async void LaunchAtLogin_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        bool want = LaunchAtLogin.IsChecked == true;
        bool actual = await StartupManager.SetEnabledAsync(want);
        _loading = true;
        LaunchAtLogin.IsChecked = actual;
        _loading = false;
        if (want && !actual)
            SetStatus("Launch at login was blocked (check Startup apps in Settings).");
        else
            SetStatus(actual ? "Will launch at login." : "Launch at login disabled.");
    }

    // ---- Test visualizer -------------------------------------------------
    private void OnTriggerPressed(string triggerName)
        => DispatcherQueue.TryEnqueue(() =>
        {
            PenIndicator.Background = ActiveBrush;
            PenIndicator.BorderBrush = ActiveBrush;
            PenIndicatorText.Text = triggerName;
            PenIndicatorSub.Text = "Pen button pressed";
            AddLogEntry($"Pen button: {triggerName}");
            _penResetTimer.Stop();
            _penResetTimer.Start();
        });

    private void OnHotkeyFired(string hotkey)
        => DispatcherQueue.TryEnqueue(() =>
        {
            HotkeyIndicator.Background = FireBrush;
            HotkeyIndicator.BorderBrush = FireBrush;
            HotkeyIndicatorText.Text = hotkey;
            HotkeyIndicatorSub.Text = "Hotkey sent";
            AddLogEntry($"Hotkey fired: {hotkey}");
            _hotkeyResetTimer.Stop();
            _hotkeyResetTimer.Start();
        });

    private void ResetPenIndicator()
    {
        PenIndicator.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        PenIndicator.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        PenIndicatorText.Text = "Waiting…";
        PenIndicatorSub.Text = "No pen button pressed";
    }

    private void ResetHotkeyIndicator()
    {
        HotkeyIndicator.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        HotkeyIndicator.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        HotkeyIndicatorSub.Text = "Idle";
    }

    private void AddLogEntry(string message)
    {
        ActivityLog.Items.Insert(0, new TextBlock
        {
            Text = $"{DateTime.Now:HH:mm:ss.fff}  {message}",
            IsTextSelectionEnabled = true,
        });
        while (ActivityLog.Items.Count > 100)
            ActivityLog.Items.RemoveAt(ActivityLog.Items.Count - 1);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e) => ActivityLog.Items.Clear();

    // ---- Learn buttons ---------------------------------------------------
    private Task LearnTriggerAsync(TextBox target)
        => CaptureAsync(target, isTrigger: true, "Press the pen button now…");

    private Task LearnHotkeyAsync(TextBox target)
        => CaptureAsync(target, isTrigger: false, "Press the hotkey combo you want to SEND…");

    private async Task CaptureAsync(TextBox target, bool isTrigger, string prompt)
    {
        SetStatus(prompt);
        Engine.Learning = true;
        try
        {
            string? captured = isTrigger
                ? await KeyCapture.CaptureTriggerAsync(8000)
                : await KeyCapture.CaptureHotkeyAsync(8000);

            if (string.IsNullOrEmpty(captured))
            {
                SetStatus("Nothing captured (timed out).");
                return;
            }
            target.Text = captured;
            SetStatus($"Captured: {captured}. Click Save.");
        }
        finally
        {
            Engine.Learning = false;
        }
    }

    private async void SingleTriggerLearn_Click(object sender, RoutedEventArgs e) => await LearnTriggerAsync(SingleTrigger);
    private async void SingleHotkeyLearn_Click(object sender, RoutedEventArgs e) => await LearnHotkeyAsync(SingleHotkey);
    private async void DoubleTriggerLearn_Click(object sender, RoutedEventArgs e) => await LearnTriggerAsync(DoubleTrigger);
    private async void DoubleHotkeyLearn_Click(object sender, RoutedEventArgs e) => await LearnHotkeyAsync(DoubleHotkey);
    private async void LongTriggerLearn_Click(object sender, RoutedEventArgs e) => await LearnTriggerAsync(LongTrigger);
    private async void LongHotkeyLearn_Click(object sender, RoutedEventArgs e) => await LearnHotkeyAsync(LongHotkey);
}
