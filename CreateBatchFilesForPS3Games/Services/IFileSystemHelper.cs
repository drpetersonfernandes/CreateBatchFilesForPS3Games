namespace CreateBatchFilesForPS3Games.Services;

public interface IFileSystemHelper
{
    bool VerifyWriteAccess(string folderPath);
}
