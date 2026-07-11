namespace SlimPenHotkeys.Core;

/// <summary>
/// Maps AutoHotkey-style key names (e.g. "F20", "Space", "Volume_Up") to Win32
/// virtual-key codes and back. Keeping the AHK naming means settings files and
/// the README's hotkey syntax remain familiar to existing users.
/// </summary>
internal static class KeyMap
{
    // Virtual key codes.
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_LSHIFT = 0xA0;
    public const int VK_RSHIFT = 0xA1;
    public const int VK_LMENU = 0xA4;   // Left Alt
    public const int VK_RMENU = 0xA5;   // Right Alt
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;
    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_MENU = 0x12;    // Alt
    public const int VK_P = 0x50;

    private static readonly Dictionary<string, int> NameToVk = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> VkToName = new();

    static KeyMap()
    {
        // Letters A-Z -> 0x41..0x5A
        for (char c = 'A'; c <= 'Z'; c++)
            Add(c.ToString(), c);
        // Digits 0-9 -> 0x30..0x39
        for (char c = '0'; c <= '9'; c++)
            Add(c.ToString(), c);
        // Function keys F1-F24 -> 0x70..0x87
        for (int i = 1; i <= 24; i++)
            Add("F" + i, 0x70 + (i - 1));

        Add("Space", 0x20);
        Add("Enter", 0x0D);
        Add("Return", 0x0D);
        Add("Tab", 0x09);
        Add("Escape", 0x1B);
        Add("Esc", 0x1B);
        Add("Backspace", 0x08);
        Add("Up", 0x26);
        Add("Down", 0x28);
        Add("Left", 0x25);
        Add("Right", 0x27);
        Add("Home", 0x24);
        Add("End", 0x23);
        Add("PgUp", 0x21);
        Add("PgDn", 0x22);
        Add("Insert", 0x2D);
        Add("Delete", 0x2E);
        Add("Del", 0x2E);

        // Media / browser / volume / launch keys.
        Add("Volume_Up", 0xAF);
        Add("Volume_Down", 0xAE);
        Add("Volume_Mute", 0xAD);
        Add("Media_Play_Pause", 0xB3);
        Add("Media_Next", 0xB0);
        Add("Media_Prev", 0xB1);
        Add("Media_Stop", 0xB2);
        Add("Browser_Back", 0xA6);
        Add("Browser_Forward", 0xA7);
        Add("Browser_Refresh", 0xA8);
        Add("Browser_Home", 0xAC);
        Add("Launch_Mail", 0xB4);
        Add("Launch_Media", 0xB5);
        Add("Launch_App1", 0xB6);
        Add("Launch_App2", 0xB7);
    }

    private static void Add(string name, int vk)
    {
        NameToVk[name] = vk;
        if (!VkToName.ContainsKey(vk))
            VkToName[vk] = name;
    }

    public static bool TryGetVk(string name, out int vk)
        => NameToVk.TryGetValue(name.Trim(), out vk);

    public static string? GetName(int vk)
        => VkToName.TryGetValue(vk, out var name) ? name : null;

    /// <summary>Whether a virtual key needs the extended-key flag when injected.</summary>
    public static bool IsExtended(int vk) => vk switch
    {
        0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 => true, // nav cluster
        0x2D or 0x2E => true,           // Insert / Delete
        VK_RCONTROL or VK_RMENU => true,
        VK_LWIN or VK_RWIN => true,
        >= 0xA6 and <= 0xB7 => true,    // browser / volume / media / launch
        _ => false,
    };
}
