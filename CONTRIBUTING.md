# Contributing to WinTextCapture

Thanks for taking the time. WinTextCapture is a small, single-purpose tool, and it should stay that way: fixes,
clearer messages, better OCR handling and translations are very welcome; large new features are best discussed
in an issue first.

## Build and test

You need Windows 10 2004 (build 19041) or later and the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```powershell
dotnet build WinTextCapture.csproj -c Release
dotnet run -c Release

dotnet build tests\WinTextCapture.Tests\WinTextCapture.Tests.csproj -c Release
dotnet run --project tests\WinTextCapture.Tests\WinTextCapture.Tests.csproj -c Release --no-build
```

The test program has no extra packages. It prints passed and failed counts and exits with `0` only when
everything passed. Some checks open test windows briefly; it never starts the tray app and never touches the
startup registry entry. Live desktop use, mixed-DPI monitors, OCR accuracy and a busy clipboard still need a
person to try, so please say in your pull request what you tried by hand.

## Pull requests

- Branch from `master`, keep each pull request to one change, and describe what and why.
- Add or update a check in `tests/WinTextCapture.Tests/Program.cs` when you change behaviour that can be tested
  without a desktop (settings, hotkey state, message text, preprocessing, start-with-Windows decisions).
- Every user-facing string lives in `Strings.cs`. Add it to both the English and the Turkish table.
- Match the existing style: nullable reference types on, file-scoped namespaces, small focused classes.
- If you change how a window looks, regenerate the pictures in `docs/` with the `--screenshot` modes
  (see the README, "Build from source").
- No network code, telemetry or new runtime dependencies, please.

## Be kind

Be patient and respectful in issues and reviews. Everyone here is volunteering their time.
