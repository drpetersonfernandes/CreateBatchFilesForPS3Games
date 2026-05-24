namespace CreateBatchFilesForPS3Games;

public interface IFileSystemHelper
{
    bool VerifyWriteAccess(string folderPath);
}
