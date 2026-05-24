using System.IO;

namespace CreateBatchFilesForPS3Games.Services;

public class FileSystemHelper : IFileSystemHelper
{
    public bool VerifyWriteAccess(string folderPath)
    {
        try
        {
            var testFile = Path.Combine(folderPath, ".temp_test_" + Guid.NewGuid().ToString("N")[..8] + ".tmp");
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
