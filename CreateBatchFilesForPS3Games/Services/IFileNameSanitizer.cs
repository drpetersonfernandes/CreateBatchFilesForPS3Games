namespace CreateBatchFilesForPS3Games.Services;

public interface IFileNameSanitizer
{
    string SanitizeFileName(string filename);
    bool IsRomanNumeral(string word);
}
