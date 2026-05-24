using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace CreateBatchFilesForPS3Games;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";

        if (App.NewVersionAvailable && App.LatestVersion != null)
        {
            UpdateStatusTextBlock.Text = $"A new version ({App.LatestVersion}) is available!";
            UpdateStatusTextBlock.Foreground = FindResource("SuccessTextBrush") as System.Windows.Media.Brush;
            ShowReleaseLink(App.ReleaseUrl);
        }
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusTextBlock.Inlines.Clear();
            UpdateStatusTextBlock.Text = "Checking for updates...";
            UpdateStatusTextBlock.Foreground = FindResource("TextSecondaryBrush") as System.Windows.Media.Brush;

            try
            {
                var service = App.UpdateService;
                if (service == null)
                {
                    UpdateStatusTextBlock.Text = "Update service is not available.";
                    return;
                }

                var currentVersion = new Version(GetApplicationVersion());
                var (updateAvailable, latestVersion, releaseUrl) = await service.CheckForUpdateAsync(currentVersion);

                UpdateStatusTextBlock.Inlines.Clear();

                if (updateAvailable && latestVersion != null)
                {
                    UpdateStatusTextBlock.Text = $"A new version ({latestVersion}) is available!";
                    UpdateStatusTextBlock.Foreground = FindResource("SuccessTextBrush") as System.Windows.Media.Brush;
                    ShowReleaseLink(releaseUrl);
                }
                else
                {
                    UpdateStatusTextBlock.Text = "You are running the latest version.";
                    UpdateStatusTextBlock.Foreground = FindResource("TextSecondaryBrush") as System.Windows.Media.Brush;
                }
            }
            catch
            {
                UpdateStatusTextBlock.Inlines.Clear();
                UpdateStatusTextBlock.Text = "Failed to check for updates. Please try again later.";
                UpdateStatusTextBlock.Foreground = FindResource("FailedTextBrush") as System.Windows.Media.Brush;
            }
            finally
            {
                CheckUpdatesButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            _ = App.BugReportService?.SendBugReportAsync("Failed to check for updates.", GetApplicationVersion(), ex);
        }
    }

    private void ShowReleaseLink(string? releaseUrl)
    {
        if (string.IsNullOrWhiteSpace(releaseUrl))
            return;

        var lineBreak = new Run("\n") { FontSize = 13 };
        UpdateStatusTextBlock.Inlines.Add(lineBreak);

        var hyperlink = new Hyperlink
        {
            NavigateUri = new Uri(releaseUrl),
            Foreground = FindResource("LinkTextBrush") as System.Windows.Media.Brush,
            TextDecorations = TextDecorations.Underline
        };
        hyperlink.Inlines.Add("Download from GitHub");
        hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
        UpdateStatusTextBlock.Inlines.Add(hyperlink);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // Notify developer
            if (App.BugReportService != null)
            {
                _ = App.BugReportService.SendBugReportAsync($"Error opening URL: {e.Uri.AbsoluteUri}", GetApplicationVersion(), ex);
            }

            // Notify user
            MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            e.Handled = true;
        }
    }
}