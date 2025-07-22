using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CreateBatchFilesForPS3Games;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        LogMessage("Welcome to the Batch File Creator for PS3 Games.");
        LogMessage("");
        LogMessage("This program creates batch files to launch your PS3 games.");
        LogMessage("Please follow these steps:");
        LogMessage("1. Select the RPCS3 emulator executable file (rpcs3.exe)");
        LogMessage("2. Select the root folder where you want to save the batch files");
        LogMessage("3. Click 'Create Batch Files' to generate the batch files");
        LogMessage("");
        UpdateStatusBarMessage("Ready");
    }

    private void UpdateStatusBarMessage(string message)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // The application will shut down automatically when the main window closes.
        // No extra code is needed here.
    }

    private void LogMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
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
        var rootFolder = SelectFolder();
        if (string.IsNullOrEmpty(rootFolder)) return;

        GameFolderTextBox.Text = rootFolder;
        LogMessage($"Batch file output folder selected: {rootFolder}");
        UpdateStatusBarMessage("Output folder selected.");
    }

    private async void CreateBatchFilesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var rpcs3ExePath = Rpcs3PathTextBox.Text;
            var outputFolder = GameFolderTextBox.Text;

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

            CreateBatchFilesButton.IsEnabled = false;
            UpdateStatusBarMessage("Processing... please wait.");

            try
            {
                var totalFilesCreated = 0;
                var totalFoldersScanned = 0;

                // Process main game folder (if different from output folder)
                var rpcs3Root = Path.GetDirectoryName(rpcs3ExePath);
                if (rpcs3Root == null)
                {
                    ShowError("Could not determine the RPCS3 root directory.");
                    UpdateStatusBarMessage("Error: Could not determine RPCS3 root.");
                    return;
                }

                // Process dev_hdd0/game folder inside RPCS3 directory
                var rpcs3GameFolder = Path.Combine(rpcs3Root, "dev_hdd0", "game");
                if (Directory.Exists(rpcs3GameFolder))
                {
                    LogMessage($"\n--- Scanning RPCS3 game folder: {rpcs3GameFolder} ---\n");
                    var (scanned, created) = await ProcessGameFoldersAsync(rpcs3GameFolder, rpcs3ExePath, outputFolder, GameType.HddGame);
                    totalFoldersScanned += scanned;
                    totalFilesCreated += created;
                }
                else
                {
                    LogMessage($"\n--- RPCS3 game folder not found at {rpcs3GameFolder}, skipping. ---\n");
                }

                // Process the user-selected "Games Folder" as a source of disc games
                LogMessage($"\n--- Scanning disc game folder: {outputFolder} ---\n");
                var (discScanned, discCreated) = await ProcessGameFoldersAsync(outputFolder, rpcs3ExePath, outputFolder, GameType.DiscGame);
                totalFoldersScanned += discScanned;
                totalFilesCreated += discCreated;


                LogMessage("\n--- Process Complete ---");
                LogMessage($"Scanned {totalFoldersScanned} potential game folders.");
                LogMessage($"Successfully created {totalFilesCreated} batch files in '{outputFolder}'.");
                UpdateStatusBarMessage($"Process complete. Created {totalFilesCreated} files.");

                ShowMessageBox($"Batch file creation complete.\n\nCreated {totalFilesCreated} files.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
            }
        }
        catch (Exception ex)
        {
            _ = ReportBugAsync("Error creating batch files", ex);
        }
    }

    private enum GameType
    {
        DiscGame,
        HddGame
    }

    private async Task<(int foldersScanned, int filesCreated)> ProcessGameFoldersAsync(string sourceFolder, string rpcs3ExePath, string outputFolder, GameType type)
    {
        var filesCreated = 0;
        var subdirectories = await Task.Run(() => Directory.GetDirectories(sourceFolder));

        foreach (var subdirectory in subdirectories)
        {
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

            var sfoData = await Task.Run(() => ReadSfo(sfoPath));
            if (sfoData == null)
            {
                LogMessage($"Could not read PARAM.SFO for {Path.GetFileName(subdirectory)}, skipping.");
                continue;
            }

            sfoData.TryGetValue("TITLE", out var title);
            sfoData.TryGetValue("TITLE_ID", out var titleId);

            var batchFileName = !string.IsNullOrEmpty(title) ? title :
                !string.IsNullOrEmpty(titleId) ? titleId :
                Path.GetFileName(subdirectory);

            batchFileName = SanitizeFileName(batchFileName);
            var batchFilePath = Path.Combine(outputFolder, batchFileName + ".bat");

            try
            {
                await using var sw = new StreamWriter(batchFilePath);
                var rpcs3Directory = Path.GetDirectoryName(rpcs3ExePath);
                await sw.WriteLineAsync("@echo off");
                await sw.WriteLineAsync($"cd /d \"{rpcs3Directory}\"");
                await sw.WriteLineAsync($"start \"\" \"{rpcs3ExePath}\" --no-gui \"{ebootPath}\"");
                LogMessage($"Batch file created: {batchFilePath}");
                filesCreated++;
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to create batch file for {Path.GetFileName(subdirectory)}: {ex.Message}");
                await ReportBugAsync($"Failed to create batch file for {batchFileName}", ex);
            }
        }

        return (subdirectories.Length, filesCreated);
    }

    private static string? SelectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Please select the folder where you want to save the batch files."
        };
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

    private static readonly char[] Separator = [' ', '.', '-', '_', ':'];

    private string SanitizeFileName(string filename)
    {
        filename = filename.Replace("™", "").Replace("®", "").Replace(":", " -");
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(filename.Split(invalidChars));
    }

    private Dictionary<string, string>? ReadSfo(string sfoFilePath)
    {
        if (!File.Exists(sfoFilePath)) return null;

        try
        {
            var result = new Dictionary<string, string>();
            var sfoBytes = File.ReadAllBytes(sfoFilePath);

            // Basic validation
            if (sfoBytes.Length < 20 || BitConverter.ToUInt32(sfoBytes, 0) != 0x46535000) // PSF magic
            {
                LogMessage($"Invalid SFO file header: {sfoFilePath}");
                return null;
            }

            var keyTableStart = BitConverter.ToUInt32(sfoBytes, 8);
            var dataTableStart = BitConverter.ToUInt32(sfoBytes, 12);
            var tablesEntries = BitConverter.ToUInt32(sfoBytes, 16);

            for (var i = 0; i < tablesEntries; i++)
            {
                var entryOffset = 20 + (i * 16);
                var keyOffset = BitConverter.ToUInt16(sfoBytes, entryOffset);
                var dataFormat = BitConverter.ToUInt16(sfoBytes, entryOffset + 2);
                var dataLength = BitConverter.ToUInt32(sfoBytes, entryOffset + 4);
                var dataOffset = BitConverter.ToUInt32(sfoBytes, entryOffset + 12);

                var key = ReadNullTerminatedString(sfoBytes, (int)(keyTableStart + keyOffset));
                var value = "";

                if ((dataFormat & 0xFF) == 0x04) // Is string type
                {
                    value = ReadNullTerminatedString(sfoBytes, (int)(dataTableStart + dataOffset), (int)dataLength);
                }
                else if (dataFormat == 0x0404) // Is integer type
                {
                    value = BitConverter.ToUInt32(sfoBytes, (int)(dataTableStart + dataOffset)).ToString(CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(key))
                {
                    result.TryAdd(key, value);
                }
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

    private static string ReadNullTerminatedString(byte[] buffer, int offset, int maxLength = -1)
    {
        var end = Array.IndexOf(buffer, (byte)0, offset);
        if (end == -1)
        {
            end = buffer.Length;
        }

        if (maxLength != -1 && end > offset + maxLength)
        {
            end = offset + maxLength;
        }

        return Encoding.UTF8.GetString(buffer, offset, end - offset);
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Dispatcher.Invoke(() => MessageBox.Show(this, message, title, buttons, icon));
    }

    private void ShowError(string message)
    {
        ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task ReportBugAsync(string message, Exception? exception = null)
    {
        if (App.BugReportService == null) return;

        try
        {
            var fullReport = new StringBuilder();
            var assemblyName = GetType().Assembly.GetName();

            fullReport.AppendLine("=== Bug Report ===");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Application: {assemblyName.Name}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Version: {assemblyName.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now}");
            fullReport.AppendLine().AppendLine("=== Error Message ===").AppendLine(message).AppendLine();

            if (exception != null)
            {
                fullReport.AppendLine("=== Exception Details ===");
                var currentEx = exception;
                var level = 0;
                while (currentEx != null)
                {
                    var indent = new string(' ', level * 2);
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {currentEx.GetType().FullName}");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {currentEx.Message}");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"{indent}StackTrace: {currentEx.StackTrace}");
                    currentEx = currentEx.InnerException;
                    level++;
                    if (currentEx != null) fullReport.AppendLine(CultureInfo.InvariantCulture, $"{indent}Inner Exception:");
                }
            }

            var logContent = await Dispatcher.InvokeAsync(() => LogTextBox.Text);
            if (!string.IsNullOrEmpty(logContent))
            {
                fullReport.AppendLine().AppendLine("=== Application Log ===").Append(logContent);
            }

            var (rpcs3Path, gameFolderPath) = await Dispatcher.InvokeAsync(() => (Rpcs3PathTextBox.Text, GameFolderTextBox.Text));
            fullReport.AppendLine().AppendLine("=== Configuration ===").AppendLine(CultureInfo.InvariantCulture, $"RPCS3 Path: {rpcs3Path}").AppendLine(CultureInfo.InvariantCulture, $"Games Folder: {gameFolderPath}");

            await App.BugReportService.SendBugReportAsync(fullReport.ToString());
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
