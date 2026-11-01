# Shot list

The README hero, `docs/demo.gif`, is an illustration drawn from `docs/demo/scene.html`, not a recording. The PNGs in
`docs/` are rendered by the app's own `--screenshot` modes. This list covers the real recordings that are still
missing. Until they exist, `docs/media/*.placeholder.svg` stand in for them; none is shown in the README.

## General setup (all scenes)

- Windows 11, display scale **100 %**, a plain dark wallpaper, taskbar auto-hidden or cropped out.
- Close or hide anything personal: no real names, mail, chats, file paths with a user name, or notifications
  (turn on Do Not Disturb).
- Use made-up text only, for example the notepad page from the screenshots: "Invoice number: INV-2026-0418",
  "Delivery date: 14 November, 10:30 am", "Address: 12 Harbour Street, Portsmouth".
- Run the current `master` build so the look matches the screenshots. App theme: **Dark**, interface language
  English, Recognition language `English (en-US)`.
- Record a fixed **1280x720** region at **15 fps** (ScreenToGif or similar), cursor visible. Export GIF with a
  shared palette; aim for under 3 MB. Also keep the MP4 if your recorder makes one.

## Scene 1: capture, then paste (hero)

- File: `docs/media/capture-demo.gif` (placeholder: `docs/media/capture-demo.placeholder.svg`)
- Size, length: 1280x720 region, 8–10 s, 15 fps, dark theme, cursor visible.
- Steps:
  1. Notepad open with the three made-up lines, a blank mail or second Notepad window beside it.
  2. Hold 1 s, then press `Win + Shift + T`. Hold 1 s on the dimmed screen with the hint pill visible.
  3. Drag a box slowly over the three lines so the size badge is readable; release.
  4. Let the **Copied** toast stay visible for about 2 s.
  5. Click into the second window, press `Ctrl + V`, hold 1.5 s on the pasted text.
- Swap in: in `README.md` replace `docs/demo.gif` in the hero `<img>` with `docs/media/capture-demo.gif`, update
  the alt text, and change the caption under it to "Screen recording of v<version>." Keep `docs/demo.gif` and
  `docs/demo/` or delete both together.

## Scene 2: change the hotkey and OCR language

- File: `docs/media/settings-hotkey.gif` (placeholder: `docs/media/settings-hotkey.placeholder.svg`)
- Size, length: crop to the Settings window plus a little margin (about 900x700), 8 s, 15 fps, dark theme,
  cursor visible.
- Steps:
  1. Right-click the tray icon, choose **Settings...** (record the tray menu briefly if it fits in the region).
  2. Click the hotkey box, press `Ctrl + Alt + T`, show the new combination.
  3. Open **Recognition language**, pick `English (en-US)`.
  4. Click **Save**.
- Swap in: optional. If used, add it under "Use" in `README.md` below the two settings PNGs, width 640, with the
  caption "Changing the hotkey and the OCR language."

## Stills the app can render itself (no recording needed)

Run on Windows from the build output folder; each writes a PNG off-screen with default settings:

```powershell
WinTextCapture.exe --screenshot-tray docs/tray-menu-dark.png Dark en
WinTextCapture.exe --screenshot-overlay docs/selection-dark.png Dark en
WinTextCapture.exe --screenshot-toast docs/toast-copied-dark.png Dark en copied
WinTextCapture.exe --screenshot docs/settings-dark.png Dark en
```

`docs/tray-menu-dark.png` does not exist yet; if you make it, it can sit next to the "Tray menu" line in the README
at width 260.
