# Clipboard Keeper

A small local Windows clipboard history utility for text and images.

## What it does

- Captures text and image clipboard changes while the app is running.
- Keeps the most recent 100 unique items.
- Shows image thumbnails in the list and a larger image preview.
- Opens a larger image viewer when you double-click an image preview.
- Shows the selected image's local storage path and can reveal it in File Explorer.
- Auto-sizes each main-list row by description length, up to three visible lines.
- Filters the main list by `All`, `Text`, `Image`, or `Star`.
- Uses icons for item type in the main list.
- Enlarges the preview pane when the main filter is set to `Image`.
- Lets you star important items; starred items are pinned first by newest star time.
- Lets you copy the selected item back to the clipboard with the `Copy Selected` button.
- Lets you add an optional editable `Name` to each item for quick recognition.
- Lets you multi-select and delete several items at once, or clear all saved items.
- Lets you exit explicitly with the `Exit` button.
- Minimizes to the Windows system tray and keeps monitoring there.
- Shows a compact quick panel when you click the tray icon, and restores the full window when you double-click it.
- Compact quick panel mirrors the main list with filter, star state, type icons, names, and descriptions.
- Stores history only on this computer under `%LOCALAPPDATA%\JiraAceClipboardManager`.
- Encrypts saved text and image files with the current Windows user account.
- Builds with a local app icon that represents reusable text and image clipboard content.

## Security posture

- No network access.
- No administrator permission.
- No browser injection.
- No keyboard hooks or keystroke recording.
- No auto-start registration.
- Uses standard Windows clipboard APIs.
- Saved history uses Windows DPAPI, so another Windows user cannot read it directly from the storage folder.

Because Windows does not expose past system clipboard history to regular apps, the app can only build history from the moment it is running. It captures the current clipboard once on startup, then records later clipboard changes.

## Build

Run this from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The executable is created at:

```text
bin\ClipboardKeeper.exe
```

The build script also generates and embeds:

```text
assets\ClipboardKeeper.ico
```

## Run

Open:

```text
bin\ClipboardKeeper.exe
```

Keep the window open while you want clipboard history to be captured. Use `Pause` when copying sensitive content that should not be recorded.

When you minimize the window or close it with the `X` button, it hides to the system tray instead of quitting. Click the tray icon to open the compact quick panel, or double-click it to restore the full window. Use `Main Window` in the compact panel to restore the full window, or use `Exit` to quit completely.

To name an item, select it in the main list and click `Edit Name`, or press `F2`.
To pin an item, select it and click `Star`; click `Unstar` to remove the pin.
