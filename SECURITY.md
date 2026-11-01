# Security policy

## Supported versions

Only the [latest release](https://github.com/ydbilgin/WinTextCapture/releases/latest) receives fixes.

## Reporting a vulnerability

Please do not open a public issue for a security problem. Report it privately through GitHub's private
vulnerability reporting: <https://github.com/ydbilgin/WinTextCapture/security/advisories/new>.

Include the version (shown under **About** in the tray menu), your Windows version, and steps to reproduce.
You will get a reply as soon as possible; please allow time for a fix before disclosing details.

## What the app touches

WinTextCapture has no network code. On your PC it:

- installs a low-level keyboard hook to detect the capture hotkey; other keys are passed on untouched;
- captures the screen of all monitors into memory while the capture overlay is open;
- writes the selected area to a temporary PNG in `%TEMP%` for Windows OCR and deletes it right after;
- writes the recognised text to the clipboard;
- stores `settings.json` and a size-limited `error.log` in `%APPDATA%\WinTextCapture\`;
- adds or removes a `WinTextCapture` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` only when you
  change **Start with Windows**, or to repair an entry whose program no longer exists.

It does not need administrator rights.
