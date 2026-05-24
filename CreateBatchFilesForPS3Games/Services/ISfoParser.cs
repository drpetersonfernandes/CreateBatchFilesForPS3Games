namespace CreateBatchFilesForPS3Games.Services;

public interface ISfoParser
{
    Dictionary<string, string>? ParseSfo(byte[] sfoBytes);
    string ReadNullTerminatedString(byte[] buffer, int offset, int maxLength = -1);
}
