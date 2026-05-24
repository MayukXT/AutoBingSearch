using AutoBingSearch.Core.Configuration;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace AutoBingSearch.Core.Scheduling;

public sealed class TaskSchedulerService
{
    public const string SearchTaskName = "AutoBingSearch Search";
    public const string ReminderTaskName = "AutoBingSearch Reminder";

    public IReadOnlyList<string> BuildRegisterCommands(AppConfig config, string exePath)
    {
        var commands = new List<string>();

        if (config.SearchEnabled)
            commands.Add(CreateCommand(SearchTaskName, exePath, "--run", config.SearchTime));

        if (config.ReminderEnabled)
            commands.Add(CreateCommand(ReminderTaskName, exePath, "--reminder", config.ReminderTime));

        return commands;
    }

    public async Task RegisterAsync(AppConfig config, string exePath, CancellationToken cancellationToken = default)
    {
        await DeleteTaskAsync(SearchTaskName, cancellationToken);
        await DeleteTaskAsync(ReminderTaskName, cancellationToken);

        foreach (var command in BuildRegisterCommands(config, exePath))
            await RunPowerShellAsync(command, cancellationToken);
    }

    public async Task UnregisterAsync(CancellationToken cancellationToken = default)
    {
        await DeleteTaskAsync(SearchTaskName, cancellationToken);
        await DeleteTaskAsync(ReminderTaskName, cancellationToken);
    }

    public bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void RelaunchElevated(string exePath, string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            Verb = "runas",
            UseShellExecute = true
        });
    }

    private static string CreateCommand(string taskName, string exePath, string appArg, string hhmm)
    {
        var time = TimeOnly.Parse(hhmm).ToString("HH:mm");
        return string.Join(
            Environment.NewLine,
            [
                $"$action = New-ScheduledTaskAction -Execute '{EscapePowerShell(exePath)}' -Argument '{EscapePowerShell(appArg)}'",
                $"$trigger = New-ScheduledTaskTrigger -Daily -At '{time}'",
                "$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries",
                $"Register-ScheduledTask -TaskName '{EscapePowerShell(taskName)}' -Action $action -Trigger $trigger -Settings $settings -Force | Out-Null"
            ]);
    }

    private static async Task DeleteTaskAsync(string taskName, CancellationToken cancellationToken)
    {
        try
        {
            await RunPowerShellAsync(
                $"Unregister-ScheduledTask -TaskName '{EscapePowerShell(taskName)}' -Confirm:$false -ErrorAction SilentlyContinue",
                cancellationToken);
        }
        catch
        {
            // Missing tasks are fine.
        }
    }

    private static async Task RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Could not start PowerShell.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode == 0)
            return;

        var error = await errorTask;
        var output = await outputTask;
        throw new InvalidOperationException((error + output).Trim());
    }

    private static string EscapePowerShell(string value) => value.Replace("'", "''");
}
