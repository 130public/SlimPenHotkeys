using static SlimPenHotkeys.Interop.NativeMethods;

namespace SlimPenHotkeys.Core;

/// <summary>
/// Polls physical key state to capture a trigger key or an output hotkey combo,
/// reproducing the AutoHotkey Learn buttons. Runs on a background thread; the
/// engine is put into pass-through (Learning) mode by the caller so captured
/// keys are neither suppressed nor remapped.
/// </summary>
internal static class KeyCapture
{
    private static readonly string[] TriggerKeys =
    {
        "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24",
        "Volume_Up","Volume_Down","Volume_Mute","Media_Play_Pause",
        "Media_Next","Media_Prev","Media_Stop","Browser_Back",
        "Browser_Forward","Browser_Refresh","Browser_Home",
        "Launch_Mail","Launch_Media","Launch_App1","Launch_App2",
        "Enter","Space","Escape","Tab","Up","Down","Left","Right",
        "PgUp","PgDn","Home","End","Insert","Delete",
    };

    private static readonly string[] HotkeyBaseKeys =
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "Space","Enter","Tab","Escape","Up","Down","Left","Right",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24",
        "Home","End","PgUp","PgDn","Insert","Delete",
    };

    private static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    /// <summary>Capture a single trigger key. Returns its AHK name or null on timeout.</summary>
    public static Task<string?> CaptureTriggerAsync(int timeoutMs)
        => Task.Run(() =>
        {
            long deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline)
            {
                foreach (var name in TriggerKeys)
                {
                    if (KeyMap.TryGetVk(name, out int vk) && Down(vk))
                        return name;
                }
                Thread.Sleep(15);
            }
            return (string?)null;
        });

    /// <summary>Capture an output hotkey combo. Returns AHK syntax (e.g. "^+{F9}") or null.</summary>
    public static Task<string?> CaptureHotkeyAsync(int timeoutMs)
        => Task.Run(() =>
        {
            long deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline)
            {
                foreach (var name in HotkeyBaseKeys)
                {
                    if (!KeyMap.TryGetVk(name, out int vk) || !Down(vk))
                        continue;

                    string mods = "";
                    if (Down(KeyMap.VK_CONTROL)) mods += "^";
                    if (Down(KeyMap.VK_SHIFT)) mods += "+";
                    if (Down(KeyMap.VK_MENU)) mods += "!";
                    if (Down(KeyMap.VK_LWIN) || Down(KeyMap.VK_RWIN)) mods += "#";

                    // Wait for the base key to be released before returning.
                    while (Down(vk)) Thread.Sleep(10);
                    return $"{mods}{{{name}}}";
                }
                Thread.Sleep(15);
            }
            return (string?)null;
        });
}
