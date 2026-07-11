using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SlimPenHotkeys;

/// <summary>
/// Custom entry point (replaces the XAML-generated Main via
/// DISABLE_XAML_GENERATED_MAIN) so we can enforce a single running instance.
/// A named mutex guards the primary instance; secondary launches signal the
/// primary to surface its window and then exit.
/// </summary>
public static class Program
{
    private const string MutexName = "SlimPenHotkeys_SingleInstance_Mutex_v1";
    private const string ShowEventName = "SlimPenHotkeys_Show_Event_v1";

    /// <summary>Raised on a background thread when another instance asks us to show.</summary>
    public static event Action? ShowSignal;

    private static Mutex? _mutex;
    private static EventWaitHandle? _showEvent;

    [STAThread]
    private static int Main(string[] args)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out var ev))
                {
                    ev.Set();
                    ev.Dispose();
                }
            }
            catch { /* primary may be mid-startup; nothing else to do */ }
            return 0;
        }

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        StartShowListener();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });

        return 0;
    }

    private static void StartShowListener()
    {
        var t = new Thread(() =>
        {
            while (true)
            {
                _showEvent!.WaitOne();
                ShowSignal?.Invoke();
            }
        })
        {
            IsBackground = true,
            Name = "SlimPenHotkeys.ShowListener",
        };
        t.Start();
    }
}
