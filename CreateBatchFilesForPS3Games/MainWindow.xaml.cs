using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using CreateBatchFilesForPS3Games.Models;
using CreateBatchFilesForPS3Games.Services;
using Microsoft.Win32;

namespace CreateBatchFilesForPS3Games;

public partial class MainWindow
{
    private readonly ISfoParser _sfoParser;
    private readonly IFileNameSanitizer _fileNameSanitizer;
    private readonly IFileSystemHelper _fileSystemHelper;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private bool _isClosing;

    public MainWindow(ISfoParser sfoParser, IFileNameSanitizer fileNameSanitizer, IFileSystemHelper fileSystemHelper)
    {
        InitializeComponent();
        _sfoParser = sfoParser;
        _fileNameSanitizer = fileNameSanitizer;
        _fileSystemHelper = fileSystemHelper;
        LogMessage("Welcome to the Batch File Creator for PS3 Games.");
        LogMessage("");
        LogMessage("This program creates batch files to launch your PS3 games.");
        LogMessage("Please follow these steps:");
        LogMessage("1. Select the RPCS3 emulator executable file (rpcs3.exe)");
        LogMessage("2. (Optional) Select the folder containing your PS3 disc game folders");
        LogMessage("3. Select the folder where you want to save the batch files");
        LogMessage("4. Click 'Create Batch Files' to generate the batch files");
        LogMessage("");
        UpdateStatusBarMessage("Ready");
    }

    internal void ShowUpdateAvailable(string? latestVersion, string? releaseUrl)
    {
        if (string.IsNullOrWhiteSpace(latestVersion)) return;

        UpdateStatusBarMessage($"A new version ({latestVersion}) is available!");

        var result = MessageBox.Show(
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
                _ = App.BugReportService?.SendBugReportAsync($"Error opening URL: {releaseUrl}", "unknown", ex);
            }
        }
    }

    private void UpdateStatusBarMessage(string message)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            if (_isClosing)
                return;

            if (_processingTask is { IsCompleted: false })
            {
                e.Cancel = true;

                _cts?.Cancel();
                CreateBatchFilesButton.IsEnabled = false;
                UpdateStatusBarMessage("Waiting for running tasks to complete...");

                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    // ignored
                }

                _isClosing = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error in method Window_Closing", ex);
        }
    }

    private void LogMessage(string message)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        dispatcher.InvokeAsync(() =>
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private void BrowseRPCS3Button_Click(object sender, RoutedEventArgs e)
    {
        var rpcs3ExePath = SelectFile();
        if (string.IsNullOrEmpty(rpcs3ExePath)) return;

        Rpcs3PathTextBox.Text = rpcs3ExePath;
        LogMessage($"RPCS3 executable selected: {rpcs3ExePath}");
        UpdateStatusBarMessage("RPCS3 executable selected.");
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var discGamesFolder = SelectFolder("Please select the folder containing your PS3 disc game folders.");
        if (string.IsNullOrEmpty(discGamesFolder)) return;

        GameFolderTextBox.Text = discGamesFolder;
        LogMessage($"Disc games folder selected: {discGamesFolder}");
        UpdateStatusBarMessage("Disc games folder selected.");
    }

    private void BrowseOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var outputFolder = SelectFolder("Please select the folder where you want to save the batch files.");
        if (string.IsNullOrEmpty(outputFolder)) return;

        OutputFolderTextBox.Text = outputFolder;
        LogMessage($"Batch file output folder selected: {outputFolder}");
        UpdateStatusBarMessage("Output folder selected.");
    }

    private async void CreateBatchFilesButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var rpcs3ExePath = Rpcs3PathTextBox.Text;
                var gamesFolder = GameFolderTextBox.Text;
                var outputFolder = OutputFolderTextBox.Text;

                if (string.IsNullOrEmpty(rpcs3ExePath) || !File.Exists(rpcs3ExePath))
                {
                    ShowError("Please select a valid RPCS3 executable file (rpcs3.exe).");
                    UpdateStatusBarMessage("Error: Invalid RPCS3 path.");
                    return;
                }

                if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
                {
                    ShowError("Please select a valid folder to save the batch files.");
                    UpdateStatusBarMessage("Error: Invalid output folder path.");
                    return;
                }

                if (!_fileSystemHelper.VerifyWriteAccess(outputFolder))
                {
                    LogMessage($"Write permission check failed for '{outputFolder}'.");
                    ShowError("Cannot write to the selected folder. Please try these solutions:\n\n" +
                              "1. Run the application as Administrator\n" +
                              "2. Choose a different output folder (e.g., your Desktop or Documents)\n" +
                              "3. Check the folder security permissions in Windows Explorer");
                    UpdateStatusBarMessage("Error: Insufficient folder permissions.");
                    return;
                }

                if (rpcs3ExePath.Contains('"') || outputFolder.Contains('"') || (gamesFolder.Contains('"')))
                {
                    ShowError("File paths containing double quotes are not supported. Please select valid paths.");
                    UpdateStatusBarMessage("Error: Paths must not contain double quotes.");
                    return;
                }

                CreateBatchFilesButton.IsEnabled = false;
                UpdateStatusBarMessage("Processing... please wait.");

                try
                {
                    async Task ProcessAllAsync()
                    {
                        var totalFilesCreated = 0;
                        var totalFoldersScanned = 0;

                        var rpcs3Root = Path.GetDirectoryName(rpcs3ExePath);
                        if (rpcs3Root == null)
                        {
                            ShowError("Could not determine the RPCS3 root directory.");
                            UpdateStatusBarMessage("Error: Could not determine RPCS3 root.");
                            return;
                        }

                        var rpcs3GameFolder = Path.Combine(rpcs3Root, "dev_hdd0", "game");
                        if (Directory.Exists(rpcs3GameFolder))
                        {
                            LogMessage($"\n--- Scanning RPCS3 game folder: {rpcs3GameFolder} ---\n");
                            var (scanned, created) = await ProcessGameFoldersAsync(rpcs3GameFolder, rpcs3ExePath, outputFolder, GameType.HddGame, token);
                            totalFoldersScanned += scanned;
                            totalFilesCreated += created;
                        }
                        else
                        {
                            LogMessage($"\n--- RPCS3 game folder not found at {rpcs3GameFolder}, skipping. ---\n");
                        }

                        if (!string.IsNullOrEmpty(gamesFolder) && Directory.Exists(gamesFolder))
                        {
                            LogMessage($"\n--- Scanning disc game folder: {gamesFolder} ---\n");
                            var (discScanned, discCreated) = await ProcessGameFoldersAsync(gamesFolder, rpcs3ExePath, outputFolder, GameType.DiscGame, token);
                            totalFoldersScanned += discScanned;
                            totalFilesCreated += discCreated;
                        }

                        LogMessage("\n--- Process Complete ---");
                        LogMessage($"Scanned {totalFoldersScanned} potential game folders.");
                        LogMessage($"Successfully created {totalFilesCreated} batch files in '{outputFolder}'.");
                        UpdateStatusBarMessage($"Process complete. Created {totalFilesCreated} files.");

                        ShowMessageBox($"Batch file creation complete.\n\nCreated {totalFilesCreated} files.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    _processingTask = ProcessAllAsync();
                    await _processingTask;
                }
                catch (Exception ex)
                {
                    LogMessage($"An unexpected error occurred: {ex.Message}");
                    _ = ReportBugAsync("An unexpected error occurred during batch file creation.", ex);
                    ShowError($"An unexpected error occurred: {ex.Message}");
                    UpdateStatusBarMessage("An error occurred.");
                }
                finally
                {
                    CreateBatchFilesButton.IsEnabled = true;
                    _cts?.Dispose();
                    _cts = null;
                    _processingTask = null;
                }
            }
            catch (Exception ex)
            {
                _ = ReportBugAsync("Error creating batch files", ex);
                _cts?.Dispose();
                _cts = null;
                _processingTask = null;
            }
        }
        catch (Exception ex)
        {
            _ = ReportBugAsync("Error creating batch files", ex);
        }
    }

    private async Task<(int foldersScanned, int filesCreated)> ProcessGameFoldersAsync(string sourceFolder, string rpcs3ExePath, string outputFolder, GameType type, CancellationToken token)
    {
        var filesCreated = 0;
        var foldersScanned = 0;
        var subdirectories = await Task.Run(() => Directory.GetDirectories(sourceFolder), token);

        foreach (var subdirectory in subdirectories)
        {
            token.ThrowIfCancellationRequested();
            string ebootPath;
            string sfoPath;

            if (type == GameType.DiscGame)
            {
                ebootPath = Path.Combine(subdirectory, "PS3_GAME", "USRDIR", "EBOOT.BIN");
                sfoPath = Path.Combine(subdirectory, "PS3_GAME", "PARAM.SFO");
            }
            else // HddGame
            {
                ebootPath = Path.Combine(subdirectory, "USRDIR", "EBOOT.BIN");
                sfoPath = Path.Combine(subdirectory, "PARAM.SFO");
            }

            if (!File.Exists(ebootPath) || !File.Exists(sfoPath))
            {
                continue; // Not a valid game folder for this type
            }

            foldersScanned++;

            var sfoData = await Task.Run(() => ReadSfo(sfoPath), token);
            if (sfoData == null)
            {
                LogMessage($"Could not read PARAM.SFO for {Path.GetFileName(subdirectory)}, skipping.");
                continue;
            }

            sfoData.TryGetValue("TITLE", out var title);
            sfoData.TryGetValue("TITLE_ID", out var titleId);

            var batchFileName = !string.IsNullOrEmpty(title) ? title :
                !string.IsNullOrEmpty(titleId) ? titleId :
                !string.IsNullOrEmpty(Path.GetFileName(subdirectory)) ? Path.GetFileName(subdirectory) :
                "UntitledGame";

            batchFileName = _fileNameSanitizer.SanitizeFileName(batchFileName);
            var batchFilePath = Path.Combine(outputFolder, batchFileName + ".bat");

            try
            {
                // Check and handle existing files
                if (File.Exists(batchFilePath))
                {
                    try
                    {
                        // Remove the read-only attribute if present
                        var fileInfo = new FileInfo(batchFilePath);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }

                        File.Delete(batchFilePath);
                    }
                    catch (Exception deleteEx)
                    {
                        LogMessage($"⚠️ Skipping '{batchFileName}.bat': Cannot overwrite existing file - {deleteEx.Message}");
                        continue; // Skip this file and continue with others
                    }
                }

                // Create the batch file with explicit sharing permissions
                await using (var fs = new FileStream(batchFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                await using (var sw = new StreamWriter(fs))
                {
                    var rpcs3Directory = Path.GetDirectoryName(rpcs3ExePath);
                    await sw.WriteLineAsync("@echo off");
                    await sw.WriteLineAsync($"cd /d \"{EscapeBatchPath(rpcs3Directory!)}\"");
                    await sw.WriteLineAsync($"start \"\" \"{EscapeBatchPath(rpcs3ExePath)}\" --no-gui \"{EscapeBatchPath(ebootPath)}\"");
                }

                LogMessage($"✓ Batch file created: {batchFilePath}");
                filesCreated++;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogMessage($"❌ Access denied for '{batchFileName}.bat': {ex.Message}");
                LogMessage("   → Try running the application as Administrator");
                // Don't report permission issues as bugs - they're user environment issues
            }
            catch (IOException ex)
            {
                LogMessage($"❌ IO error creating '{batchFileName}.bat': {ex.Message}");
                await ReportBugAsync($"IO error creating batch file: {batchFileName}", ex);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to create batch file for {Path.GetFileName(subdirectory)}: {ex.Message}");
                await ReportBugAsync($"Unexpected error creating batch file: {batchFileName}", ex);
            }
        }

        return (foldersScanned, filesCreated);
    }

    private static string? SelectFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private static string? SelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Please select the RPCS3 emulator executable file (rpcs3.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    internal static string EscapeBatchPath(string path)
    {
        return path.Replace("\"", "");
    }

    private Dictionary<string, string>? ReadSfo(string sfoFilePath)
    {
        if (!File.Exists(sfoFilePath)) return null;

        try
        {
            var sfoBytes = File.ReadAllBytes(sfoFilePath);
            var result = _sfoParser.ParseSfo(sfoBytes);

            if (result == null)
            {
                LogMessage($"Invalid SFO file header: {sfoFilePath}");
            }

            return result;
        }
        catch (Exception ex)
        {
            LogMessage($"Error reading SFO file '{sfoFilePath}': {ex.Message}");
            _ = ReportBugAsync($"Error parsing SFO file: {sfoFilePath}", ex);
            return null;
        }
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var dispatcher = Dispatcher;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        dispatcher.Invoke(() => MessageBox.Show(this, message, title, buttons, icon));
    }

    private void ShowError(string message)
    {
        ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task ReportBugAsync(string message, Exception? exception = null)
    {
        if (App.BugReportService == null) return;

        if (exception is UnauthorizedAccessException)
        {
            return;
        }

        try
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

            var fullMessage = new StringBuilder();
            fullMessage.AppendLine(message);

            string? logContent = null;
            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                try
                {
                    logContent = await Dispatcher.InvokeAsync(() => LogTextBox.Text);
                }
                catch
                {
                    // Window may have been closed/disposed
                }
            }

            if (!string.IsNullOrEmpty(logContent))
            {
                fullMessage.AppendLine().AppendLine("=== Application Log ===").Append(logContent);
            }

            var rpcs3Path = "";
            var gameFolderPath = "";
            var outputFolderPath = "";
            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                (rpcs3Path, gameFolderPath, outputFolderPath) = await Dispatcher.InvokeAsync(() => (Rpcs3PathTextBox.Text, GameFolderTextBox.Text, OutputFolderTextBox.Text));
            }

            fullMessage.AppendLine().AppendLine("=== Configuration ===")
                .AppendLine(CultureInfo.InvariantCulture, $"RPCS3 Path: {rpcs3Path}")
                .AppendLine(CultureInfo.InvariantCulture, $"Disc Games Folder: {gameFolderPath}")
                .AppendLine(CultureInfo.InvariantCulture, $"Output Folder: {outputFolderPath}");

            await App.BugReportService.SendBugReportAsync(fullMessage.ToString(), version, exception);
        }
        catch
        {
            // Silently fail if error reporting itself fails
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            LogMessage($"Error opening About window: {ex.Message}");
            _ = ReportBugAsync("Error opening About window", ex);
        }
    }
}
