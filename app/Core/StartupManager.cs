using Windows.ApplicationModel;

namespace SlimPenHotkeys.Core;

/// <summary>
/// Manages launch-at-login for the packaged app via the Windows StartupTask API.
/// The task is declared in Package.appxmanifest (windows.startupTask extension).
/// </summary>
internal static class StartupManager
{
    public const string TaskId = "SlimPenHotkeysStartup";

    public static async Task<bool> IsEnabledAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Enable or disable launch-at-login. Returns the resulting enabled state.</summary>
    public static async Task<bool> SetEnabledAsync(bool enable)
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            if (enable)
            {
                var state = await task.RequestEnableAsync();
                return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            task.Disable();
            return false;
        }
        catch
        {
            return false;
        }
    }
}
