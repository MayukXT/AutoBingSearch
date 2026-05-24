using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Scheduling;

namespace AutoBingSearch.Tests;

public sealed class TaskSchedulerServiceTests
{
    [Fact]
    public void BuildRegisterCommands_IncludesEnabledSearchAndReminder()
    {
        var config = new AppConfig
        {
            SearchTime = "22:00",
            ReminderTime = "22:30",
            SearchEnabled = true,
            ReminderEnabled = true
        };

        var commands = new TaskSchedulerService().BuildRegisterCommands(config, "C:\\Apps\\AutoBingSearch.exe");

        Assert.Contains(commands, c =>
            c.Contains("--run") &&
            c.Contains("New-ScheduledTaskTrigger -Daily -At '22:00'") &&
            c.Contains("-StartWhenAvailable"));
        Assert.Contains(commands, c =>
            c.Contains("--reminder") &&
            c.Contains("New-ScheduledTaskTrigger -Daily -At '22:30'") &&
            c.Contains("-StartWhenAvailable"));
    }

    [Fact]
    public void BuildRegisterCommands_SkipsDisabledTasks()
    {
        var config = new AppConfig { SearchEnabled = false, ReminderEnabled = true };

        var commands = new TaskSchedulerService().BuildRegisterCommands(config, "AutoBingSearch.exe");

        Assert.Single(commands);
        Assert.Contains("--reminder", commands[0]);
    }
}
