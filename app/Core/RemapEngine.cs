using System.Runtime.InteropServices;
using SlimPenHotkeys.Interop;
using static SlimPenHotkeys.Interop.NativeMethods;

namespace SlimPenHotkeys.Core;

/// <summary>
/// Installs a global low-level keyboard hook and remaps the Surface Pen gesture
/// keys (F20/F19/F18 by default) to configurable output hotkeys, firing on key
/// release. Also reproduces the original script's LWin-suppression heuristic so
/// Windows can't swallow the pen's Win+F-key combo or pop the Start menu.
/// </summary>
internal sealed class RemapEngine
{
    private enum LWinState { Idle, Suppressed, Replayed }

    private readonly object _sync = new();
    private readonly LowLevelKeyboardProc _proc;   // kept alive to avoid GC
    private readonly System.Threading.Timer _replayTimer;

    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hHook;

    private Dictionary<int, Hotkey> _triggers = new();
    private int _holdMs = 120;
    private LWinState _lwin = LWinState.Idle;

    /// <summary>Raised (on the hook thread) when Ctrl+Alt+P is pressed.</summary>
    public event Action? ShowWindowRequested;

    /// <summary>Raised (on the hook thread) when a mapped pen trigger key goes
    /// down. The argument is the trigger's key name (e.g. "F20").</summary>
    public event Action<string>? TriggerPressed;

    /// <summary>Raised (on the hook thread) when a mapped pen trigger key is
    /// released and its hotkey is fired. The argument is the hotkey's
    /// human-readable form (e.g. "Ctrl+Shift+F9").</summary>
    public event Action<string>? HotkeyFired;

    /// <summary>When true the hook passes all keys through untouched (used while
    /// the UI is capturing a key for the Learn buttons).</summary>
    public bool Learning { get; set; }

    public RemapEngine()
    {
        _proc = HookProc;
        _replayTimer = new System.Threading.Timer(OnReplayTimer, null, Timeout.Infinite, Timeout.Infinite);
    }

    public bool IsActive
    {
        get { lock (_sync) return _triggers.Count > 0; }
    }

    public void ApplyConfig(AppSettings settings)
    {
        var map = new Dictionary<int, Hotkey>();
        foreach (var g in new[] { settings.Single, settings.Double, settings.Long })
        {
            if (!g.Enabled || string.IsNullOrWhiteSpace(g.Trigger) || string.IsNullOrWhiteSpace(g.Hotkey))
                continue;
            if (!KeyMap.TryGetVk(g.Trigger, out int vk))
                continue;
            if (!Hotkey.TryParse(g.Hotkey, out var hk) || hk is null)
                continue;
            map[vk] = hk;
        }

        lock (_sync)
        {
            _triggers = map;
            _holdMs = settings.HoldMs;
            _lwin = LWinState.Idle;
            _replayTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(HookThreadMain) { IsBackground = true, Name = "SlimPenHotkeys.Hook" };
        _thread.Start();
    }

    public void Stop()
    {
        if (_thread is null) return;
        if (_threadId != 0)
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(2000);
        _thread = null;
    }

    private void HookThreadMain()
    {
        _threadId = GetCurrentThreadId();
        _hHook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);

        while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_hHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hHook);
            _hHook = IntPtr.Zero;
        }
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == HC_ACTION)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool injected = (data.flags & LLKHF_INJECTED) != 0 || data.dwExtraInfo == InjectSignature;
            if (!injected)
            {
                int msg = (int)wParam;
                bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
                bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;
                if (HandleKey((int)data.vkCode, isDown, isUp))
                    return (IntPtr)1; // suppress
            }
        }
        return CallNextHookEx(_hHook, nCode, wParam, lParam);
    }

    private bool HandleKey(int vk, bool isDown, bool isUp)
    {
        if (Learning)
            return false; // let keys pass through untouched while capturing

        // Global show hotkey: Ctrl+Alt+P (works regardless of active mappings).
        if (isDown && vk == KeyMap.VK_P && IsDown(KeyMap.VK_CONTROL) && IsDown(KeyMap.VK_MENU))
        {
            ShowWindowRequested?.Invoke();
            return true;
        }

        Hotkey? trigger;
        bool active;
        lock (_sync)
        {
            active = _triggers.Count > 0;
            _triggers.TryGetValue(vk, out trigger);
        }
        if (!active)
            return false;

        if (vk == KeyMap.VK_LWIN)
            return HandleLWin(isDown, isUp);

        if (trigger is not null)
            return HandleTrigger(vk, trigger, isDown, isUp);

        return false;
    }

    private bool HandleTrigger(int vk, Hotkey hotkey, bool isDown, bool isUp)
    {
        if (isDown)
        {
            TriggerPressed?.Invoke(KeyMap.GetName(vk) ?? ("VK" + vk));

            // Pen F-key arrived: the LWin that came with it was from the pen, so
            // keep it suppressed and cancel the pending replay.
            lock (_sync)
            {
                _replayTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _lwin = LWinState.Idle;
            }
        }
        else if (isUp)
        {
            int hold;
            lock (_sync) hold = _holdMs;
            var hk = hotkey;
            HotkeyFired?.Invoke(hk.Describe());
            Task.Run(() =>
            {
                try { hk.Send(hold); }
                catch { /* best-effort synthesis */ }
            });
        }
        return true; // suppress the trigger key entirely
    }

    private bool HandleLWin(bool isDown, bool isUp)
    {
        lock (_sync)
        {
            if (isDown)
            {
                if (_lwin == LWinState.Idle)
                {
                    _lwin = LWinState.Suppressed;
                    _replayTimer.Change(30, Timeout.Infinite);
                }
            }
            else if (isUp)
            {
                _replayTimer.Change(Timeout.Infinite, Timeout.Infinite);
                switch (_lwin)
                {
                    case LWinState.Replayed:
                        InputSender.SendKey(KeyMap.VK_LWIN, false);
                        break;
                    case LWinState.Suppressed:
                        // Quick real Win tap (no pen key) — replay the whole tap.
                        InputSender.SendKey(KeyMap.VK_LWIN, true);
                        InputSender.SendKey(KeyMap.VK_LWIN, false);
                        break;
                    // Idle: consumed by a pen key — swallow, inject nothing.
                }
                _lwin = LWinState.Idle;
            }
        }
        return true; // always manage LWin ourselves while active
    }

    private void OnReplayTimer(object? state)
    {
        lock (_sync)
        {
            if (_lwin == LWinState.Suppressed)
            {
                _lwin = LWinState.Replayed;
                InputSender.SendKey(KeyMap.VK_LWIN, true);
            }
        }
    }

    private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
}
