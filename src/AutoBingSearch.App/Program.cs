namespace AutoBingSearch.App;

using AutoBingSearch.Core;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(initiallyOwned: true, "AutoBingSearch.App", out var ownsMutex);
        if (!ownsMutex && args.Length == 0)
            return;

        AppPaths.EnsureBaseDirectories();
        var runner = new CommandRunner(args);
        runner.Run();
    }
}
