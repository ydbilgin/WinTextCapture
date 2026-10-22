using System.Globalization;

namespace WinTextCapture;

/// <summary>
/// Tiny in-memory localization table (no .resx, no satellite assemblies). Add a language by adding
/// one dictionary and a case in <see cref="Use"/>/<see cref="Resolve"/>.
/// </summary>
public static class Strings
{
    /// <summary>Apply a language. value is "auto" | "en" | "tr" (anything else -> en).</summary>
    public static void Use(string language) => _t = Resolve(language) == "tr" ? Tr : En;

    /// <summary>"auto" resolves to the OS UI culture (Turkish -> tr, everything else -> en).</summary>
    public static string Resolve(string language)
    {
        if (language is "tr" or "en") return language;
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";
    }

    private static string G(string key) =>
        _t.TryGetValue(key, out var v) ? v : (En.TryGetValue(key, out var e) ? e : key);

    // ---- settings dialog ----
    public static string SettingsTitle => G("SettingsTitle");
    public static string HotkeyHead => G("HotkeyHead");
    public static string HotkeyHint => G("HotkeyHint");
    public static string HotkeyPressKeys => G("HotkeyPressKeys");
    public static string HotkeyListening => G("HotkeyListening");
    public static string Clear => G("Clear");
    public static string OcrHead => G("OcrHead");
    public static string OcrLangLabel => G("OcrLangLabel");
    public static string OcrLangAuto => G("OcrLangAuto");
    public static string GeneralHead => G("GeneralHead");
    public static string StartWithWindows => G("StartWithWindows");
    public static string AppearanceHead => G("AppearanceHead");
    public static string LanguageLabel => G("LanguageLabel");
    public static string ThemeLabel => G("ThemeLabel");
    public static string LangAuto => G("LangAuto");
    public static string ThemeSystem => G("ThemeSystem");
    public static string ThemeLight => G("ThemeLight");
    public static string ThemeDark => G("ThemeDark");
    public static string Save => G("Save");
    public static string Cancel => G("Cancel");
    public static string ValidationPickKey => G("ValidationPickKey");

    // ---- tray / notifications ----
    public static string OverlayHint => G("OverlayHint");
    public static string MenuCapture => G("MenuCapture");
    public static string MenuSettings => G("MenuSettings");
    public static string MenuAbout => G("MenuAbout");
    public static string MenuExit => G("MenuExit");
    public static string Copied => G("Copied");
    public static string CopiedPreview => G("CopiedPreview");
    public static string NoText => G("NoText");
    public static string ClipboardUnchanged => G("ClipboardUnchanged");
    public static string CaptureError => G("CaptureError");
    public static string CaptureBusy => G("CaptureBusy");
    public static string AboutTitle => G("AboutTitle");
    public static string TipAutoStartFail => G("TipAutoStartFail");
    public static string SettingsSaveFailed => G("SettingsSaveFailed");

    // ---- interpolated (keep {0} out of the table) ----
    public static string TrayText(string combo) => string.Format(G("TrayText"), combo);
    public static string TipHotkeyFail(string combo) => string.Format(G("TipHotkeyFail"), combo);
    public static string AboutDialogBody(string combo) => string.Format(G("AboutDialogBody"), combo, App.Version);

    private static readonly Dictionary<string, string> En = new()
    {
        ["SettingsTitle"] = "WinTextCapture — Settings",
        ["SettingsSaveFailed"] = "Settings could not be saved. Your previous settings are still active.",
        ["HotkeyHead"] = "Capture hotkey",
        ["HotkeyHint"] = "Click the box, then press your key combo. It overrides Windows' own shortcut.",
        ["HotkeyPressKeys"] = "(press keys)",
        ["HotkeyListening"] = "Press keys",
        ["Clear"] = "Clear",
        ["OcrHead"] = "OCR",
        ["OcrLangLabel"] = "Recognition language",
        ["OcrLangAuto"] = "Automatic (profile)",
        ["GeneralHead"] = "General",
        ["StartWithWindows"] = "Start with Windows",
        ["AppearanceHead"] = "Appearance",
        ["LanguageLabel"] = "Language",
        ["ThemeLabel"] = "Theme",
        ["LangAuto"] = "Automatic",
        ["ThemeSystem"] = "Follow system",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["Save"] = "Save",
        ["Cancel"] = "Cancel",
        ["ValidationPickKey"] = "Please pick a key and at least one modifier (Ctrl / Alt / Shift / Win).",
        ["OverlayHint"] = "Drag over text · Esc to cancel",
        ["MenuCapture"] = "Capture now",
        ["MenuSettings"] = "Settings...",
        ["MenuAbout"] = "About",
        ["MenuExit"] = "Exit",
        ["Copied"] = "Text copied to clipboard.",
        ["CopiedPreview"] = "Copied: {0}",
        ["NoText"] = "No readable text found. Clipboard unchanged.",
        ["ClipboardUnchanged"] = "OCR found text, but clipboard did not change.",
        ["CaptureError"] = "Capture failed.",
        ["CaptureBusy"] = "Capture is already running.",
        ["AboutTitle"] = "About WinTextCapture",
        ["TrayText"] = "WinTextCapture — {0}",
        ["TipHotkeyFail"] = "Couldn't register hotkey: {0}",
        ["TipAutoStartFail"] = "Couldn't update the Windows startup setting.",
        ["AboutDialogBody"] = "WinTextCapture v{1}\n\nNative Windows OCR — screen text to clipboard.\n\nHotkey: {0}\nDrag a box over text, release to copy. Esc cancels.",
    };

    private static readonly Dictionary<string, string> Tr = new()
    {
        ["SettingsTitle"] = "WinTextCapture — Ayarlar",
        ["SettingsSaveFailed"] = "Ayarlar kaydedilemedi. Önceki ayarlarınız etkin kalmaya devam ediyor.",
        ["HotkeyHead"] = "Yakalama kısayolu",
        ["HotkeyHint"] = "Kutuya tıklayın, sonra tuş kombinasyonuna basın. Windows'un kendi kısayolunu ezer.",
        ["HotkeyPressKeys"] = "(tuşlara basın)",
        ["HotkeyListening"] = "Tuşlara basın",
        ["Clear"] = "Temizle",
        ["OcrHead"] = "OCR",
        ["OcrLangLabel"] = "Tanıma dili",
        ["OcrLangAuto"] = "Otomatik (profil)",
        ["GeneralHead"] = "Genel",
        ["StartWithWindows"] = "Windows ile birlikte başlat",
        ["AppearanceHead"] = "Görünüm",
        ["LanguageLabel"] = "Dil",
        ["ThemeLabel"] = "Tema",
        ["LangAuto"] = "Otomatik",
        ["ThemeSystem"] = "Sisteme uy",
        ["ThemeLight"] = "Açık",
        ["ThemeDark"] = "Koyu",
        ["Save"] = "Kaydet",
        ["Cancel"] = "İptal",
        ["ValidationPickKey"] = "Lütfen bir tuş ve en az bir değiştirici (Ctrl / Alt / Shift / Win) seçin.",
        ["OverlayHint"] = "Metnin üzerinden sürükleyin · İptal için Esc",
        ["MenuCapture"] = "Şimdi yakala",
        ["MenuSettings"] = "Ayarlar...",
        ["MenuAbout"] = "Hakkında",
        ["MenuExit"] = "Çıkış",
        ["Copied"] = "Metin panoya kopyalandı.",
        ["CopiedPreview"] = "Kopyalandı: {0}",
        ["NoText"] = "Okunabilir metin bulunamadı. Pano değişmedi.",
        ["ClipboardUnchanged"] = "OCR metin buldu ama pano değişmedi.",
        ["CaptureError"] = "Yakalama başarısız.",
        ["CaptureBusy"] = "Yakalama zaten çalışıyor.",
        ["AboutTitle"] = "WinTextCapture Hakkında",
        ["TrayText"] = "WinTextCapture — {0}",
        ["TipHotkeyFail"] = "Kısayol kaydedilemedi: {0}",
        ["TipAutoStartFail"] = "Windows başlangıç ayarı güncellenemedi.",
        ["AboutDialogBody"] = "WinTextCapture v{1}\n\nYerleşik Windows OCR — ekrandaki metni panoya.\n\nKısayol: {0}\nMetnin üzerine kutu çizip bırakın, kopyalanır. Esc iptal eder.",
    };

    private static Dictionary<string, string> _t = En;

    /// <summary>Keys present in one language table but not the other (tests keep this empty).</summary>
    internal static IReadOnlyList<string> MissingKeys() =>
        En.Keys.Except(Tr.Keys).Concat(Tr.Keys.Except(En.Keys)).ToList();
}
