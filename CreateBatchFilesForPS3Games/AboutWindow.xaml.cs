using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace CreateBatchFilesForPS3Games;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";
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

            try
            {
                var service = App.UpdateService;
                if (service == null)
                {
                    return;
                }

                var currentVersion = new Version(GetApplicationVersion());
                var (updateAvailable, latestVersion, releaseUrl) = await service.CheckForUpdateAsync(currentVersion);

                if (updateAvailable && latestVersion != null)
                {
                    var result = MessageBox.Show(
                        this,
                        $"A new version ({latestVersion}) is available!\n\nWould you like to go to the release page?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrWhiteSpace(releaseUrl))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            _ = App.BugReportService?.SendBugReportAsync($"Error opening URL: {releaseUrl}", GetApplicationVersion(), ex);
                        }
                    }
                }
            }
            catch
            {
                // ignored
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            if (App.BugReportService != null)
            {
                _ = App.BugReportService.SendBugReportAsync($"Error opening URL: {e.Uri.AbsoluteUri}", GetApplicationVersion(), ex);
            }

            MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            e.Handled = true;
        }
    }
}