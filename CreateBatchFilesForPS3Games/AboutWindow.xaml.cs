using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

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
                _ = App.BugReportService.SendBugReportAsync($"Error opening URL: {e.Uri.AbsoluteUri}. Exception: {ex.Message}");
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