using SlimPenHotkeys.Interop;

namespace SlimPenHotkeys.Core;

/// <summary>
/// Parses an AutoHotkey-style hotkey string (e.g. "^+{F9}", "!a", "#{Space}")
/// into modifiers + a base key, and synthesizes it via SendInput using the same
/// technique as the original script: release all physical modifiers first, press
/// modifiers + base, hold, then release in reverse so no phantom modifier leaks
/// into the target (important for Electron apps like Wispr Flow).
/// </summary>
internal sealed class Hotkey
{
    private const string ModChars = "^+!#";

    public bool Ctrl { get; private set; }
    public bool Shift { get; private set; }
    public bool Alt { get; private set; }
    public bool Win { get; private set; }
    public int BaseVk { get; private set; }
    public string BaseName { get; private set; } = "";

    public static bool TryParse(string? spec, out Hotkey? hotkey)
    {
        hotkey = null;
        if (string.IsNullOrWhiteSpace(spec))
            return false;

        string s = spec.Trim();
        var hk = new Hotkey();

        int i = 0;
        while (i < s.Length && ModChars.IndexOf(s[i]) >= 0)
        {
            switch (s[i])
            {
                case '^': hk.Ctrl = true; break;
                case '+': hk.Shift = true; break;
                case '!': hk.Alt = true; break;
                case '#': hk.Win = true; break;
            }
            i++;
        }

        string rest = s.Substring(i).Trim();
        if (rest.Length >= 2 && rest[0] == '{' && rest[^1] == '}')
            rest = rest.Substring(1, rest.Length - 2).Trim();

        if (rest.Length == 0 || !KeyMap.TryGetVk(rest, out int vk))
            return false;

        hk.BaseVk = vk;
        hk.BaseName = KeyMap.GetName(vk) ?? rest;
        hotkey = hk;
        return true;
    }

    /// <summary>Human-readable form, e.g. "Ctrl+Shift+F9".</summary>
    public string Describe()
    {
        var parts = new List<string>(5);
        if (Ctrl) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");
        if (Win) parts.Add("Win");
        parts.Add(string.IsNullOrEmpty(BaseName) ? "?" : BaseName);
        return string.Join("+", parts);
    }

    /// <summary>Fire the combo. <paramref name="holdMs"/> is how long the base key is held down.</summary>
    public void Send(int holdMs)
    {
        if (holdMs < 1) holdMs = 1;

        // 1) Force all real modifiers up so a physically-held key (or the pen's
        //    LWin) can't leak into the synthesized combo.
        InputSender.SendKeys(new[]
        {
            (KeyMap.VK_LCONTROL, false), (KeyMap.VK_RCONTROL, false),
            (KeyMap.VK_LSHIFT, false),   (KeyMap.VK_RSHIFT, false),
            (KeyMap.VK_LMENU, false),    (KeyMap.VK_RMENU, false),
            (KeyMap.VK_LWIN, false),     (KeyMap.VK_RWIN, false),
        });

        // 2) Press modifiers, then the base key.
        var down = new List<(int vk, bool isDown)>();
        if (Ctrl) down.Add((KeyMap.VK_LCONTROL, true));
        if (Shift) down.Add((KeyMap.VK_LSHIFT, true));
        if (Alt) down.Add((KeyMap.VK_LMENU, true));
        if (Win) down.Add((KeyMap.VK_LWIN, true));
        down.Add((BaseVk, true));
        InputSender.SendKeys(down);

        Thread.Sleep(holdMs);

        // 3) Release base, then modifiers in reverse order.
        var up = new List<(int vk, bool isDown)> { (BaseVk, false) };
        if (Win) up.Add((KeyMap.VK_LWIN, false));
        if (Alt) up.Add((KeyMap.VK_LMENU, false));
        if (Shift) up.Add((KeyMap.VK_LSHIFT, false));
        if (Ctrl) up.Add((KeyMap.VK_LCONTROL, false));
        InputSender.SendKeys(up);
    }
}
