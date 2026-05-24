using AutoBingSearch.Core.Logging;
using System.Diagnostics;
using System.Management;

namespace AutoBingSearch.Core.Search;

internal static class BrowserProcessCleaner
{
    private const string PidFileName = "automation-browser.pid";
    private static readonly string[] BrowserProcessNames = ["msedge.exe", "chrome.exe", "brave.exe"];

    public static void KillForProfile(string profileRoot, AppLogger log)
    {
        KillRecordedProcess(profileRoot, log);
        KillProcessesByCommandLine(profileRoot, log);
    }

    public static void WritePid(string profileRoot, int pid, AppLogger log)
    {
        try
        {
            Directory.CreateDirectory(profileRoot);
            File.WriteAllText(PidPath(profileRoot), pid.ToString());
        }
        catch (Exception ex)
        {
            log.Warn($"Could not write browser pid file: {ex.Message}");
        }
    }

    public static void RemovePid(string profileRoot)
    {
        try
        {
            var path = PidPath(profileRoot);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Non-critical cleanup.
        }
    }

    public static void KillProcessTree(Process process, AppLogger? log = null)
    {
        try
        {
            if (process.HasExited)
                return;

            process.Kill(entireProcessTree: true);
            process.WaitForExit(3000);
            log?.Info($"Stopped stale automation browser process {process.Id}.");
        }
        catch (Exception ex)
        {
            log?.Warn($"Could not stop browser process {process.Id}: {ex.Message}");
        }
    }

    private static void KillRecordedProcess(string profileRoot, AppLogger log)
    {
        var pidPath = PidPath(profileRoot);
        if (!File.Exists(pidPath))
            return;

        try
        {
            var text = File.ReadAllText(pidPath).Trim();
            if (int.TryParse(text, out var pid))
            {
                using var process = Process.GetProcessById(pid);
                KillProcessTree(process, log);
            }
        }
        catch (ArgumentException)
        {
            // The recorded process has already exited.
        }
        catch (Exception ex)
        {
            log.Warn($"Could not clean recorded browser process: {ex.Message}");
        }
        finally
        {
            RemovePid(profileRoot);
        }
    }

    private static void KillProcessesByCommandLine(string profileRoot, AppLogger log)
    {
        var normalizedProfile = Path.GetFullPath(profileRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE " +
                string.Join(" OR ", BrowserProcessNames.Select(name => $"Name='{name}'")));

            foreach (ManagementObject processInfo in searcher.Get().Cast<ManagementObject>())
            {
                var commandLine = processInfo["CommandLine"]?.ToString();
                if (string.IsNullOrWhiteSpace(commandLine))
                    continue;

                if (!commandLine.Contains(normalizedProfile, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var pid = Convert.ToInt32(processInfo["ProcessId"]);
                    using var process = Process.GetProcessById(pid);
                    KillProcessTree(process, log);
                }
                catch (ArgumentException)
                {
                    // Browser child exited between the WMI snapshot and the kill attempt.
                }
                catch (Exception ex)
                {
                    log.Warn($"Could not stop stale automation browser process: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Warn($"Could not scan for stale automation browser processes: {ex.Message}");
        }
    }

    private static string PidPath(string profileRoot) => Path.Combine(profileRoot, PidFileName);
}
