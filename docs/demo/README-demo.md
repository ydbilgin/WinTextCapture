# The README demo film

`docs/demo.gif` is drawn, not recorded. `scene.html` is a small web page that paints any moment of the
film when asked (`window.renderAt(seconds)`), and `render.mjs` steps through it frame by frame in a headless
Chromium and lets ffmpeg turn the frames into an optimised GIF. Nobody's screen is captured.

## Re-render in one command

From the repository root:

```powershell
node docs/demo/render.mjs --sheet
```

This rewrites `docs/demo.gif` and writes `docs/demo/contact-sheet.png` (one frame per second, for a quick
look; it is git-ignored). Add `--mp4` for `docs/demo.mp4` as well.

Needs Node.js 18 or newer, ffmpeg on `PATH`, and Playwright with a Chromium:

```powershell
npm install --no-save playwright
npx playwright install chromium
```

Optional environment variables: `DEMO_CHROMIUM` (path to a Chromium or chrome-headless-shell),
`FFMPEG` (path to ffmpeg), `DEMO_PLAYWRIGHT_FROM` (a folder whose `node_modules` already holds Playwright).

## Changing the film

- **Words and timing:** edit the `TIMELINE` block at the top of the script in `scene.html`. `CAPTIONS` holds
  each caption with its start and end second, `SEL` the dragged box, `T` when the hotkey, overlay, drag,
  toast and paste happen and which layer (capture, setup, end card) is visible, and `PASTED` the pasted text.
  `window.DEMO` sets size, frame rate and length.
- **The app pictures:** the toast (`docs/toast-copied-dark.png`) and the settings window
  (`docs/settings-dark.png`) are the app's own pictures. After a UI change, regenerate them with
  `WinTextCapture.exe --screenshot-toast docs/toast-copied-dark.png Dark en copied` and
  `WinTextCapture.exe --screenshot docs/settings-dark.png Dark en`, then re-render.
- **The overlay** (dimmed screen, blue box, size badge, hint pill) is drawn in `scene.html` to match
  `OverlayForm.cs`; if the overlay's look or the hint text in `Strings.cs` changes, update it here too.

Open `scene.html` in a browser and call `renderAt(5)` in the console to check a single moment.
Keep the GIF under about 3 MB so GitHub shows it quickly.
