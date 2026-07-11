using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlimPenHotkeys.Core;

/// <summary>One pen gesture mapping: trigger key -> output hotkey, plus enabled flag.</summary>
public sealed class GestureConfig
{
    public string Trigger { get; set; } = "";
    public string Hotkey { get; set; } = "";
    public bool Enabled { get; set; }

    public GestureConfig Clone() => new() { Trigger = Trigger, Hotkey = Hotkey, Enabled = Enabled };
}

/// <summary>
/// Source-generated JSON metadata for <see cref="AppSettings"/>. Using the
/// source generator (instead of reflection-based serialization) keeps settings
/// load/save working in the trimmed, self-contained Store build — reflection
/// serialization emits IL2026 trim warnings and can fail at runtime once unused
/// members are trimmed.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Persisted application settings. Stored as JSON in the packaged app's local
/// data folder. Values keep AutoHotkey hotkey syntax (^ Ctrl, + Shift, ! Alt,
/// # Win, {F9}) so the README documentation still applies.
/// </summary>
public sealed class AppSettings
{
    public GestureConfig Single { get; set; } = new();
    public GestureConfig Double { get; set; } = new();
    public GestureConfig Long { get; set; } = new();
    public int HoldMs { get; set; } = 120;

    public static AppSettings Defaults() => new()
    {
        Single = new GestureConfig { Trigger = "F20", Hotkey = "^+{F9}", Enabled = true },
        Double = new GestureConfig { Trigger = "F19", Hotkey = "", Enabled = false },
        Long = new GestureConfig { Trigger = "F18", Hotkey = "", Enabled = false },
        HoldMs = 120,
    };

    private static string SettingsPath
    {
        get
        {
            string dir;
            try
            {
                // Packaged: use the app's per-user local data folder.
                dir = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                // Unpackaged fallback (e.g. running the raw exe).
                dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SlimPenHotkeys");
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            string path = SettingsPath;
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize(File.ReadAllText(path), SettingsJsonContext.Default.AppSettings);
                if (loaded is not null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch
        {
            // Fall through to defaults on any read/parse error.
        }
        return Defaults();
    }

    public void Save()
    {
        Normalize();
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, SettingsJsonContext.Default.AppSettings));
    }

    private void Normalize()
    {
        Single ??= new GestureConfig();
        Double ??= new GestureConfig();
        Long ??= new GestureConfig();
        if (HoldMs < 1) HoldMs = 120;
    }

    public bool AnyEnabled => Single.Enabled || Double.Enabled || Long.Enabled;
}
