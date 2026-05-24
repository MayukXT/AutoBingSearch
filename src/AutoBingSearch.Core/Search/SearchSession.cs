using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Logging;
using AutoBingSearch.Core.Browsers;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AutoBingSearch.Core.Search;

public sealed class SearchSession
{
    private const int ResultsTimeoutMs = 25_000;
    private const int MaxRetries = 2;
    private static readonly string[] ChallengeIndicators =
    [
        "captcha",
        "are you a human",
        "unusual traffic",
        "verify you",
        "confirm you",
        "automated"
    ];

    private readonly AppConfig _config;
    private readonly AppLogger _log;

    public SearchSession(AppConfig config, AppLogger log)
    {
        _config = config;
        _log = log;
    }

    public async Task<SearchRunResult> RunAsync(
        bool testMode,
        IProgress<SearchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.Now;
        var searchCount = testMode ? 3 : _config.SearchCount;
        var queries = QueryBank.PickDailyQueries(searchCount);
        var humanizer = new Humanizer(
            testMode ? 3 : _config.DelayMinSeconds,
            testMode ? 8 : _config.DelayMaxSeconds,
            testMode ? 3 : _config.SessionMaxMinutes,
            queries.Count);

        var completed = 0;
        var failed = 0;
        var challengeHit = false;

        _log.Info($"Starting {(testMode ? "test" : "scheduled")} run. Browser={_config.Browser.DisplayName}, Profile={_config.Browser.ProfileName}, Count={queries.Count}");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchBrowserAsync(playwright, cancellationToken);
        var context = browser.Context;
        var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();

        try
        {
            await page.GotoAsync("https://www.bing.com", new() { Timeout = 20_000 });
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 15_000 });
        }
        catch (Exception ex)
        {
            _log.Warn($"Initial Bing load warning: {ex.Message}");
        }

        for (var index = 0; index < queries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = queries[index];
            var attempt = await DoSearchAsync(page, query, humanizer, cancellationToken);

            if (attempt.Ok)
                completed++;
            else
                failed++;

            challengeHit = string.Equals(attempt.Status, "CAPTCHA", StringComparison.OrdinalIgnoreCase);
            humanizer.RecordSearchDone();
            progress?.Report(new SearchProgress(index + 1, queries.Count, query, attempt.Status));
            _log.Info($"Search {index + 1}/{queries.Count}: {attempt.Status} | {query}");

            if (challengeHit)
                break;

            if (failed >= 3)
            {
                _log.Warn("Stopping after three failed searches to avoid burning the whole session.");
                break;
            }

            if (index + 1 < queries.Count)
                await Task.Delay(humanizer.NextDelay(), cancellationToken);
        }

        return new SearchRunResult
        {
            Completed = completed,
            Failed = failed,
            ChallengeHit = challengeHit,
            Elapsed = DateTimeOffset.Now - started
        };
    }

    private async Task<SearchAttempt> DoSearchAsync(
        IPage page,
        string query,
        Humanizer humanizer,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries + 1; attempt++)
        {
            try
            {
                await page.GotoAsync(
                    CreateBingSearchUrl(query),
                    new()
                    {
                        Timeout = ResultsTimeoutMs,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });

                if (await IsChallengePageAsync(page))
                    return new SearchAttempt(false, "CAPTCHA");

                await LightResultInteractionAsync(page, cancellationToken);
                return new SearchAttempt(true, "OK");
            }
            catch (TimeoutException ex)
            {
                _log.Warn($"Search timeout on attempt {attempt}: {query} | {ex.Message}");
                if (attempt <= MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                else
                    return new SearchAttempt(false, "TIMEOUT");
            }
            catch (Exception ex) when (attempt <= MaxRetries)
            {
                _log.Warn($"Search retry {attempt}: {query} | {ex.GetType().Name}: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (Exception ex)
            {
                _log.Error($"Search failed: {query}", ex);
                return new SearchAttempt(false, "ERROR");
            }
        }

        return new SearchAttempt(false, "ERROR");
    }

    internal static string CreateBingSearchUrl(string query)
    {
        return "https://www.bing.com/search?q=" + Uri.EscapeDataString(query).Replace("%20", "+") + "&form=QBLH";
    }

    private async Task LightResultInteractionAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            await page.Mouse.WheelAsync(0, Random.Shared.Next(140, 420));
            await Task.Delay(Random.Shared.Next(350, 900), cancellationToken);

            if (Random.Shared.NextDouble() > 0.25)
                return;

            var results = await page.QuerySelectorAllAsync("li.b_algo h2 a");
            if (results.Count > 0)
                await results[Math.Min(results.Count - 1, Random.Shared.Next(0, Math.Min(4, results.Count)))].HoverAsync();
        }
        catch
        {
            // Non-critical. The result page can shift while Bing loads related content.
        }
    }

    private static async Task<bool> IsChallengePageAsync(IPage page)
    {
        try
        {
            var title = (await page.TitleAsync()).ToLowerInvariant();
            var body = await page.EvaluateAsync<string>(
                "() => document.body ? document.body.innerText.substring(0, 1500).toLowerCase() : ''");
            var text = title + " " + body;
            return ChallengeIndicators.Any(text.Contains);
        }
        catch
        {
            return false;
        }
    }

    private async Task<BrowserSessionHandle> LaunchBrowserAsync(IPlaywright playwright, CancellationToken cancellationToken)
    {
        var profileRoot = BrowserProfileSeeder.EnsureAutomationProfile(_config.Browser);
        BrowserProcessCleaner.KillForProfile(profileRoot, _log);
        var options = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = false,
            Args =
            [
                "--window-position=-32000,-32000",
                "--window-size=1366,768",
                "--no-first-run",
                "--disable-notifications",
                "--disable-popup-blocking",
                "--disable-infobars",
                "--no-default-browser-check",
                "--disable-default-apps"
            ],
            IgnoreDefaultArgs =
            [
                "--enable-automation",
                "--disable-background-networking"
            ],
            ViewportSize = new ViewportSize { Width = 1366, Height = 768 }
        };

        if (!string.IsNullOrWhiteSpace(_config.Browser.PlaywrightChannel))
            options.Channel = _config.Browser.PlaywrightChannel;
        else if (!string.IsNullOrWhiteSpace(_config.Browser.ExecutablePath))
            options.ExecutablePath = _config.Browser.ExecutablePath;

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var context = await playwright.Chromium.LaunchPersistentContextAsync(profileRoot, options);
            return BrowserSessionHandle.ForPersistentContext(context, profileRoot, _log);
        }
        catch (Exception ex)
        {
            _log.Warn($"Pipe launch failed; retrying over CDP. {ex.GetType().Name}: {ex.Message}");
            return await LaunchWithCdpAsync(playwright, profileRoot, cancellationToken);
        }
    }

    private async Task<BrowserSessionHandle> LaunchWithCdpAsync(
        IPlaywright playwright,
        string profileRoot,
        CancellationToken cancellationToken)
    {
        var executable = ResolveExecutablePath();
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new InvalidOperationException($"Could not find executable for {_config.Browser.DisplayName}.");

        var port = ReservePort();
        var args = string.Join(
            " ",
            [
                $"--remote-debugging-port={port}",
                $"--user-data-dir=\"{profileRoot}\"",
                "--window-position=-32000,-32000",
                "--window-size=1366,768",
                "--no-first-run",
                "--disable-notifications",
                "--disable-popup-blocking",
                "about:blank"
            ]);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException($"Could not start {executable}.");

        BrowserProcessCleaner.WritePid(profileRoot, process.Id, _log);
        var endpoint = $"http://127.0.0.1:{port}";
        try
        {
            await WaitForDevToolsAsync(endpoint, process, cancellationToken);
            var connected = await playwright.Chromium.ConnectOverCDPAsync(endpoint);
            var context = connected.Contexts.FirstOrDefault() ?? throw new InvalidOperationException("Browser opened without a context.");
            return BrowserSessionHandle.ForCdp(context, connected, process, profileRoot, _log);
        }
        catch
        {
            BrowserProcessCleaner.KillProcessTree(process, _log);
            BrowserProcessCleaner.KillForProfile(profileRoot, _log);
            BrowserProcessCleaner.RemovePid(profileRoot);
            throw;
        }
    }

    private string? ResolveExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(_config.Browser.ExecutablePath) &&
            File.Exists(_config.Browser.ExecutablePath))
        {
            return _config.Browser.ExecutablePath;
        }

        var browser = new BrowserCatalog()
            .GetInstalledBrowsers()
            .FirstOrDefault(b => string.Equals(b.Id, _config.Browser.BrowserId, StringComparison.OrdinalIgnoreCase));
        return browser?.ExecutablePath;
    }

    private static int ReservePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task WaitForDevToolsAsync(string endpoint, Process process, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var deadline = DateTimeOffset.Now.AddSeconds(15);
        var originalProcessExited = false;

        while (DateTimeOffset.Now < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await client.GetAsync(endpoint + "/json/version", cancellationToken);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Keep polling; Edge often needs a moment to expose the endpoint.
            }

            originalProcessExited = originalProcessExited || process.HasExited;
            await Task.Delay(250, cancellationToken);
        }

        throw originalProcessExited
            ? new TimeoutException("Timed out waiting for browser DevTools endpoint after Edge relaunched.")
            : new TimeoutException("Timed out waiting for browser DevTools endpoint.");
    }

    private sealed class BrowserSessionHandle : IAsyncDisposable
    {
        private readonly IBrowser? _browser;
        private readonly Process? _process;
        private readonly string? _profileRoot;
        private readonly AppLogger _log;

        private BrowserSessionHandle(IBrowserContext context, IBrowser? browser, Process? process, string? profileRoot, AppLogger log)
        {
            Context = context;
            _browser = browser;
            _process = process;
            _profileRoot = profileRoot;
            _log = log;
        }

        public IBrowserContext Context { get; }

        public static BrowserSessionHandle ForPersistentContext(IBrowserContext context, string profileRoot, AppLogger log)
        {
            return new BrowserSessionHandle(context, null, null, profileRoot, log);
        }

        public static BrowserSessionHandle ForCdp(IBrowserContext context, IBrowser browser, Process process, string profileRoot, AppLogger log)
        {
            return new BrowserSessionHandle(context, browser, process, profileRoot, log);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_browser is not null)
                    await _browser.CloseAsync();
                else
                    await Context.CloseAsync();
            }
            finally
            {
                if (_process is not null)
                    BrowserProcessCleaner.KillProcessTree(_process, _log);

                if (_profileRoot is not null)
                {
                    BrowserProcessCleaner.KillForProfile(_profileRoot, _log);
                    BrowserProcessCleaner.RemovePid(_profileRoot);
                }
            }
        }
    }
}
