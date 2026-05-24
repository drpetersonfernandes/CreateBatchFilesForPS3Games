using System.Globalization;
using System.Text;

namespace CreateBatchFilesForPS3Games.Services;

public class SfoParser : ISfoParser
{
    public Dictionary<string, string>? ParseSfo(byte[] sfoBytes)
    {
        if (sfoBytes.Length < 20 || BitConverter.ToUInt32(sfoBytes, 0) != 0x46535000) // PSF magic
        {
            return null;
        }

        var result = new Dictionary<string, string>();
        var keyTableStart = BitConverter.ToUInt32(sfoBytes, 8);
        var dataTableStart = BitConverter.ToUInt32(sfoBytes, 12);
        var tablesEntries = BitConverter.ToUInt32(sfoBytes, 16);

        var maxEntries = (sfoBytes.Length - 20) / 16;
        if (tablesEntries > maxEntries)
        {
            tablesEntries = (uint)maxEntries;
        }

        for (var i = 0; i < tablesEntries; i++)
        {
            var entryOffset = 20 + (i * 16);

            if (entryOffset + 16 > sfoBytes.Length)
                break;

            var keyOffset = BitConverter.ToUInt16(sfoBytes, entryOffset);
            var dataFormat = BitConverter.ToUInt16(sfoBytes, entryOffset + 2);
            var dataLength = BitConverter.ToUInt32(sfoBytes, entryOffset + 4);
            var dataOffset = BitConverter.ToUInt32(sfoBytes, entryOffset + 12);

            var keyReadOffset = (int)(keyTableStart + keyOffset);
            if (keyReadOffset >= sfoBytes.Length)
                break;

            var key = ReadNullTerminatedString(sfoBytes, keyReadOffset);
            var value = "";

            var dataReadOffset = (int)(dataTableStart + dataOffset);
            if (dataReadOffset >= sfoBytes.Length)
                break;

            if (dataFormat == 0x0404) // Is integer type
            {
                if (dataReadOffset + 4 > sfoBytes.Length)
                    break;

                value = BitConverter.ToUInt32(sfoBytes, dataReadOffset).ToString(CultureInfo.InvariantCulture);
            }
            else if ((dataFormat & 0xFF) == 0x04) // Is string type
            {
                value = ReadNullTerminatedString(sfoBytes, dataReadOffset, (int)dataLength);
            }

            if (!string.IsNullOrEmpty(key))
            {
                result.TryAdd(key, value);
            }
        }

        return result;
    }

    public string ReadNullTerminatedString(byte[] buffer, int offset, int maxLength = -1)
    {
        if (offset < 0 || offset >= buffer.Length)
        {
            return "";
        }

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
}
