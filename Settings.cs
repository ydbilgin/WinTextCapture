using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinTextCapture;

/// <summary>User settings, persisted as plain JSON in %APPDATA%\WinTextCapture\settings.json.</summary>
public sealed class Settings
{
    // capture hotkey (default: Win + Shift + T)
    public Keys HotKey { get; set; } = Keys.T;
    public bool ModCtrl { get; set; } = false;
    public bool ModAlt { get; set; } = false;
    public bool ModShift { get; set; } = true;
    public bool ModWin { get; set; } = true;

    // OCR language: "auto" = follow the user's profile languages, else a BCP-47 tag (e.g. "tr-TR", "en-US")
    public string OcrLanguage { get; set; } = "auto";

    // behaviour / appearance
    public bool AutoStart { get; set; } = false;     // launch with Windows
    public string Language { get; set; } = "auto";   // UI language: "auto" | "en" | "tr"
    public string Theme { get; set; } = "System";    // "System" | "Light" | "Dark"

    [JsonIgnore]
    public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App.Name);

    [JsonIgnore]
    public static string FilePath => Path.Combine(Dir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { /* fall through to defaults */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }

    public bool AnyModifier => ModCtrl || ModAlt || ModShift || ModWin;

    /// <summary>Human-readable combo, e.g. "Win + Shift + T".</summary>
    public string HotKeyText()
    {
        var parts = new List<string>();
        if (ModCtrl) parts.Add("Ctrl");
        if (ModAlt) parts.Add("Alt");
        if (ModShift) parts.Add("Shift");
        if (ModWin) parts.Add("Win");
        if (HotKey != Keys.None) parts.Add(HotKey.ToString());
        return parts.Count == 0 ? "(none)" : string.Join(" + ", parts);
    }
}
