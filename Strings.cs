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
    public static string AboutHead => G("AboutHead");
    public static string AboutBody => G("AboutBody");
    public static string Save => G("Save");
    public static string Cancel => G("Cancel");
    public static string ValidationPickKey => G("ValidationPickKey");

    // ---- tray / notifications ----
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

    // ---- interpolated (keep {0} out of the table) ----
    public static string TrayText(string combo) => string.Format(G("TrayText"), combo);
    public static string TipHotkeyFail(string combo) => string.Format(G("TipHotkeyFail"), combo);
    public static string AboutDialogBody(string combo) => string.Format(G("AboutDialogBody"), combo, App.Version);

    private static readonly Dictionary<string, string> En = new()
    {
        ["SettingsTitle"] = "WinTextCapture — Settings",
        ["HotkeyHead"] = "Capture hotkey",
        ["HotkeyHint"] = "Click the box, then press your key combo. It overrides Windows' own shortcut.",
        ["HotkeyPressKeys"] = "(press keys)",
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
        ["AboutHead"] = "About",
        ["AboutBody"] = "Press the hotkey, drag a box over any text, release — the text is copied to your clipboard. Press Esc to cancel.",
        ["Save"] = "Save",
        ["Cancel"] = "Cancel",
        ["ValidationPickKey"] = "Please pick a key and at least one modifier (Ctrl / Alt / Shift / Win).",
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
        ["AboutDialogBody"] = "WinTextCapture v{1}\n\nNative Windows OCR — screen text to clipboard.\n\nHotkey: {0}\nDrag a box over text, release to copy. Esc cancels.",
    };

    private static readonly Dictionary<string, string> Tr = new()
    {
        ["SettingsTitle"] = "WinTextCapture — Ayarlar",
        ["HotkeyHead"] = "Yakalama kısayolu",
        ["HotkeyHint"] = "Kutuya tıklayın, sonra tuş kombinasyonuna basın. Windows'un kendi kısayolunu ezer.",
        ["HotkeyPressKeys"] = "(tuşlara basın)",
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
        ["AboutHead"] = "Hakkında",
        ["AboutBody"] = "Kısayola basın, metnin üzerine bir kutu çizin, bırakın — metin panoya kopyalanır. İptal için Esc.",
        ["Save"] = "Kaydet",
        ["Cancel"] = "İptal",
        ["ValidationPickKey"] = "Lütfen bir tuş ve en az bir değiştirici (Ctrl / Alt / Shift / Win) seçin.",
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
        ["AboutDialogBody"] = "WinTextCapture v{1}\n\nYerleşik Windows OCR — ekrandaki metni panoya.\n\nKısayol: {0}\nMetnin üzerine kutu çizip bırakın, kopyalanır. Esc iptal eder.",
    };

    private static Dictionary<string, string> _t = En;
}
