namespace CreateBatchFilesForPS3Games;

public interface IFileNameSanitizer
{
    string SanitizeFileName(string filename);
    bool IsRomanNumeral(string word);
}
