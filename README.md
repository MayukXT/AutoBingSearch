# AutoBingSearch

AutoBingSearch is a Windows tray app for running scheduled Bing searches for Microsoft Rewards.

The app is a native C# WinForms tray utility.

## Current Status

- `src/AutoBingSearch.App` contains the Windows tray app.
- `src/AutoBingSearch.Core` contains config, browser discovery, search automation, scheduling, startup, logging, and query selection.
- `tests/AutoBingSearch.Tests` contains core behavior tests.
- Browser profile data, cookies, login databases, runtime screenshots, virtual environments, and local config files are intentionally excluded from git.

## Build

```powershell
dotnet build AutoBingSearch.slnx
dotnet test AutoBingSearch.slnx --no-build
```

## Run

```powershell
dotnet run --project src/AutoBingSearch.App -- --tray
```

The first tray launch opens setup if no config exists. Setup asks which installed browser/profile has the Bing account.

Useful commands:

```powershell
dotnet run --project src/AutoBingSearch.App -- --setup
dotnet run --project src/AutoBingSearch.App -- --test
dotnet run --project src/AutoBingSearch.App -- --run
dotnet run --project src/AutoBingSearch.App -- --install
dotnet run --project src/AutoBingSearch.App -- --uninstall
dotnet run --project src/AutoBingSearch.App -- --reminder
```

`--install` registers the tray startup shortcut and Windows scheduled tasks. If Windows denies task registration, the app asks for UAC elevation. If elevation is declined, it does not silently claim setup worked.

## Install or Update

For normal use, install the app into a stable per-user location:

```powershell
powershell -ExecutionPolicy Bypass -File tools\install.ps1
```

The installer publishes the latest build, stops any old tray process, installs to `%LOCALAPPDATA%\Programs\AutoBingSearch`, refreshes startup and scheduled tasks, creates a Start Menu shortcut, and starts the tray app again. Use `-Setup` to force the first-run browser setup screen again.

## Features

- first-run setup wizard
- Edge default browser selection with automatic real-profile selection
- Chrome and Brave detection when installed
- tray app with Run Now, Test, Settings, Show Reminder, Open Logs, Exit
- settings UI for search time, reminder time, count, delay range, browser/profile, and profile reset
- off-screen headed browser automation through Playwright
- daily Task Scheduler registration with "start when available" for missed runs
- tray startup catch-up if the app starts after the configured search time and today's run has not happened
- Startup-folder tray launch
- persistent reminder window with acknowledgement plus timed confirmation
- local logs under `%LOCALAPPDATA%\AutoBingSearch\Logs`

## Privacy

Do not commit:

- `edge_profile/`
- `config.json`
- browser `Cookies`, `Login Data`, or `Local State`
- `.venv/`
- runtime screenshots or logs

The C# app stores config under `%APPDATA%\AutoBingSearch\config.json`.
