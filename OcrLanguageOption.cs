using Windows.Media.Ocr;

namespace WinTextCapture;

internal sealed record OcrLanguageOption(string Tag, string Display)
{
    public override string ToString() => Display;

    public static List<OcrLanguageOption> Installed()
    {
        var options = new List<OcrLanguageOption> { new("auto", Strings.OcrLangAuto) };

        try
        {
            foreach (var lang in OcrEngine.AvailableRecognizerLanguages.OrderBy(l => l.DisplayName))
                options.Add(new OcrLanguageOption(lang.LanguageTag, $"{lang.DisplayName} ({lang.LanguageTag})"));
        }
        catch
        {
            // If WinRT language enumeration fails, keep the automatic option.
        }

        return options;
    }
}
