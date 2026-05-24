using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using CreateBatchFilesForPS3Games.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CreateBatchFilesForPS3Games;

public partial class App
{
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private const string BugReportApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string ApplicationName = "CreateBatchFilesForPS3Games";
    private const string StatsApiUrl = "https://www.purelogiccode.com/api/stats";
    private const string StatsApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";

    private static readonly ServiceProvider ServiceProvider;

    public static IBugReportService? BugReportService =>
        ServiceProvider.GetService<IBugReportService>();

    public static IUpdateService? UpdateService =>
        ServiceProvider.GetService<IUpdateService>();

    public static IStatsService? StatsService =>
        ServiceProvider.GetService<IStatsService>();

    public static bool NewVersionAvailable { get; private set; }
    public static string? LatestVersion { get; private set; }
    public static string? ReleaseUrl { get; private set; }

    private static string ApplicationVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A";

    static App()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IBugReportService>(
            new BugReportService(BugReportApiUrl, BugReportApiKey, ApplicationName));

        services.AddSingleton<IUpdateService>(static _ =>
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(ApplicationName, ApplicationVersion));
            return new UpdateService(httpClient, "drpetersonfernandes", "PS3BatchLauncherCreator");
        });

        services.AddSingleton<IStatsService>(
            new StatsService(StatsApiUrl, StatsApiKey, ApplicationName, ApplicationVersion));

        services.AddSingleton<ISfoParser, SfoParser>();
        services.AddSingleton<IFileNameSanitizer, FileNameSanitizer>();
        services.AddSingleton<IFileSystemHelper, FileSystemHelper>();
        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        _ = CheckForUpdatesOnStartupAsync();
        _ = SendStartupStatsAsync();
    }

    private static async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            var updateService = UpdateService;
            if (currentVersion == null || updateService == null) return;

            var (updateAvailable, latestVersion, releaseUrl) = await updateService.CheckForUpdateAsync(currentVersion);

            if (updateAvailable)
            {
                NewVersionAvailable = true;
                LatestVersion = latestVersion;
                ReleaseUrl = releaseUrl;

                await Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Current.MainWindow is MainWindow mainWindow)
                        mainWindow.ShowUpdateAvailable(latestVersion);
                });
            }
        }
        catch
        {
            // Silently ignore startup check failures
        }
    }

    private static async Task SendStartupStatsAsync()
    {
        try
        {
            var statsService = StatsService;
            if (statsService != null)
                await statsService.SendStatsAsync();
        }
        catch
        {
            // Silently ignore stats failures
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportExceptionAsync(exception, "AppDomain.UnhandledException");
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportExceptionAsync(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static async void ReportExceptionAsync(Exception exception, string source)
    {
        try
        {
            var bugReportService = BugReportService;
            if (bugReportService != null)
            {
                await bugReportService.SendBugReportAsync(
                    $"Unhandled exception caught by: {source}",
                    ApplicationVersion,
                    exception);
            }
        }
        catch
        {
            // Silently ignore any errors in the reporting process
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ServiceProvider.Dispose();
        base.OnExit(e);
    }
}
