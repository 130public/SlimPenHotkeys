using Microsoft.Win32;

namespace SlimPenHotkeys.Core;

/// <summary>
/// Resolves the correct app icon for the current Windows theme. Two variants
/// ship in Assets: a dark-glyph icon for light mode and a light-glyph icon for
/// dark mode, so the icon stays visible on any background.
/// </summary>
/// <remarks>
/// The in-app title bar follows the <b>app</b> theme (AppsUseLightTheme), while
/// the taskbar and notification-area (tray) icons sit on surfaces painted with
/// the <b>system</b> theme (SystemUsesLightTheme); these can differ when the
/// user picks a custom Windows color mode, so they are resolved separately.
/// </remarks>
internal static class ThemeIcons
{
    /// <summary>Icon used in light mode (dark glyph).</summary>
    public const string LightModeIcon = "AppIcon-light.ico";

    /// <summary>Icon used in dark mode (light glyph).</summary>
    public const string DarkModeIcon = "AppIcon-dark.ico";

    /// <summary>True when Windows apps use the light theme.</summary>
    public static bool AppIsLight() => ReadPersonalize("AppsUseLightTheme");

    /// <summary>True when the Windows shell (taskbar/tray) uses the light theme.</summary>
    public static bool SystemIsLight() => ReadPersonalize("SystemUsesLightTheme");

    /// <summary>Absolute path to the .ico for the title bar (follows the app theme).</summary>
    public static string AppIconPath() => PathFor(AppIsLight());

    /// <summary>Absolute path to the .ico for the taskbar/tray (follows the system theme).</summary>
    public static string SystemIconPath() => PathFor(SystemIsLight());

    /// <summary>ms-appx URI for the title-bar icon source (follows the app theme).</summary>
    public static Uri AppIconUri() =>
        new($"ms-appx:///Assets/{(AppIsLight() ? LightModeIcon : DarkModeIcon)}");

    private static string PathFor(bool light) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", light ? LightModeIcon : DarkModeIcon);

    private static bool ReadPersonalize(string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue(valueName) is int i ? i != 0 : true;
        }
        catch
        {
            return true; // default to light
        }
    }
}
