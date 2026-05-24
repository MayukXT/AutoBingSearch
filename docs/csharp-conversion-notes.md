# C# Conversion Notes

Date: 2026-05-23

The previous implementation has been removed. Development targets the C# solution:

- `AutoBingSearch.App`: WinForms entrypoint, tray, setup, settings, reminder
- `AutoBingSearch.Core`: configuration, browser selection, search automation, scheduler, startup, logging
- `AutoBingSearch.Tests`: tests for deterministic core behavior

Design choices:

- Use Edge by default, because it is the common Microsoft Rewards browser.
- Detect Chrome and Brave as alternate Chromium targets.
- Keep browser profile copies under `%LOCALAPPDATA%\AutoBingSearch\BrowserProfiles`.
- Keep app config under `%APPDATA%\AutoBingSearch`.
- Keep logs under `%LOCALAPPDATA%\AutoBingSearch\Logs`.
- Use a headed off-screen browser instead of headless mode to reduce mid-session failures.
- Stop after repeated search failures so the app does not burn an entire run when Bing starts challenging the session.
- Use Windows ScheduledTasks PowerShell cmdlets for daily tasks and prompt UAC only when Windows refuses registration.
- Store a tiny local run-state file so tray startup can catch up if the machine starts after the configured search time.

The code intentionally favors small classes and direct behavior over heavy frameworks. The tray app should feel like a normal Windows utility rather than a web app packed into a desktop shell.
