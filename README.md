# Dororo on RHEL 9 / X11

A downstream port of [MelanTech/Dororo](https://github.com/MelanTech/Dororo) — a Live2D desktop pet — for **Red Hat Enterprise Linux 9** (and compatible distros: Fedora, AlmaLinux, Rocky) running an **X11 session**.

[中文版本 → README.zh.md](README.zh.md)

---

## What this is

Dororo is a desktop pet: a small animated character ("Doro") that floats on your desktop in a transparent borderless window. She wanders, follows your gaze, reacts to clicks. The upstream project targets Windows and uses Win32 APIs for the desktop-integration parts (click-through transparency, always-on-top, global cursor tracking, fullscreen detection, recycle-bin moves, login autostart).

This fork replaces every Win32-specific piece with an X11 / EWMH / freedesktop.org equivalent, while keeping the Windows build path intact:

| Original (Win32) | This fork (Linux) |
|---|---|
| `WS_EX_LAYERED + WS_EX_TRANSPARENT` click-through | `XShapeCombineRectangles(ShapeInput, …)` |
| `WS_EX_TOOLWINDOW` (hide from taskbar) | EWMH `_NET_WM_STATE_SKIP_TASKBAR` + `_NET_WM_STATE_SKIP_PAGER` |
| `GetForegroundWindow` + size compare | `_NET_ACTIVE_WINDOW` + `_NET_WM_STATE_FULLSCREEN` |
| `GetCursorPos` | `XQueryPointer(root, …)` |
| `SHFileOperation FO_DELETE` (Recycle Bin) | `gio trash` (XDG Trash spec) |
| Startup folder `.lnk` via COM | XDG Autostart `~/.config/autostart/<name>.desktop` |

The C# code branches at runtime via `OperatingSystem.IsWindows() / IsLinux()`, so a single source tree builds on both platforms.

> **Wayland is not supported.** Wayland intentionally hides the APIs the pet-window illusion depends on (click-through, global cursor query, foreground-window inspection). Log in to **GNOME on Xorg** at the GNOME login screen and verify with `echo $XDG_SESSION_TYPE` → `x11`.

---

## Quick start (pre-built binary)

Download the latest tarball from the [Releases page](../../releases) on this repo, then on a RHEL 9 X11 desktop:

```bash
sudo dnf install -y \
    libX11 libXext libXfixes libXrandr libXrender libXcursor libXinerama libXi \
    libGL libEGL fontconfig alsa-lib pulseaudio-libs glib2

tar -xzf Dororo-RHEL9.tar.gz
cd Dororo-RHEL9/prebuilt
chmod +x Dororo.x86_64
./Dororo.x86_64
```

Full deploy guide: open [`doc/deploy-guide.html`](doc/deploy-guide.html) in any browser.

---

## Build from source

If you'd rather build the binary yourself (or modify the code), follow [`doc/build-guide.html`](doc/build-guide.html) — a 16-section walkthrough for someone who's never touched Godot or Live2D before. The summary version:

1. Install `gcc-c++`, `dotnet-sdk-8.0`, `scons`, X11 / GL dev libs.
2. Install Godot 4.4 **.NET** edition.
3. Download the Live2D Cubism Native SDK version **5-r.1** from <https://www.live2d.com/sdk/download/native/> (free for personal use, license-gated).
4. Build the [MizunagiKB/gd_cubism](https://github.com/MizunagiKB/gd_cubism) GDExtension against that SDK.
5. Drop the resulting `libgd_cubism.linux.*.x86_64.so` files into `addons/gd_cubism/bin/`.
6. `dotnet build`, then `godot --headless --export-release "Linux" export/Dororo.x86_64`.

---

## Why the Live2D `.so` isn't in this repo

`gd_cubism` statically links the Live2D Cubism Native SDK, which is Live2D's proprietary code. Their license forbids redistribution of the SDK or its compiled binaries. Building from source against an SDK you accepted the EULA for yourself is fine; checking the resulting `.so` into a public repo is not.

The pre-built binary in Releases is a personal-use distribution under the same terms — if you intend to use Dororo commercially, read [Live2D's license](https://www.live2d.com/sdk/download/native/) first.

---

## What's different from upstream

Same UX as the Windows original, with these intentional downstream changes:

- **Linux/X11 support** — seven C# autoloads rewritten as dual-platform.
- **LLM chat feature removed** — the OpenAI client and chat UI are stripped (separate concern from the port; trivial to restore from upstream if you want it).
- **`window/subwindows/embed_subwindows=true`** — Settings dialog now renders inside the main window. Required for click handling on WSLg's XWayland; harmless on real X servers.
- **Wandering-Doro hit-area bug fixed** — `recalc_mouse_position` in `hit_area_handler.gd` was double-flipping the X coordinate when the character faced right, causing clicks to fall outside any hit area. (Filed upstream as a separate PR.)
- **`mouse_follow` default flipped to `true`** — pet tracks your gaze on first launch without opening settings.

See [`doc/build-guide.html`](doc/build-guide.html) § 14 for the full file-by-file diff narrative.

---

## Documentation

Open these in a browser (designed-typography HTML, not stock GitHub markdown render):

- **[`doc/index.html`](doc/index.html)** — entry point
- **[`doc/build-guide.html`](doc/build-guide.html)** — build from source, beginner level
- **[`doc/deploy-guide.html`](doc/deploy-guide.html)** — deploy the pre-built binary
- **[`doc/about.html`](doc/about.html)** — original upstream project notes

---

## Status

- ✅ Builds and runs on RHEL 9.7 (tested under WSL 2 + WSLg)
- ✅ All gestures work: drag, click expressions, mouse-follow gaze, settings dialog
- ⚠️ Click-through is **disabled by default on Linux** — the XShape ShapeInput toggle conflicted with sibling Godot Window nodes (Settings dialog) on WSLg's XWayland. On a real RHEL X11 desktop it's safe to re-enable; see the build guide.
- ⚠️ Mouse-follow tracking on WSLg is limited to the WSL X session. On native RHEL X11 it tracks globally.
- ❓ Fedora / AlmaLinux / Rocky — should work (same package names), not personally tested.

---

## Credits

- Original project: [MelanTech/Dororo](https://github.com/MelanTech/Dororo)
- Live2D GDExtension: [MizunagiKB/gd_cubism](https://github.com/MizunagiKB/gd_cubism)
- Live2D Doro character / Cubism technology: [Live2D Inc.](https://www.live2d.com/)
- Linux port and documentation: [@last-Medjay](https://github.com/last-Medjay)

## License

Same as upstream — see [`LICENSE`](LICENSE). The Live2D Cubism Native SDK has its own [proprietary terms](https://www.live2d.com/sdk/download/native/) that govern any binary `.so` built against it.
