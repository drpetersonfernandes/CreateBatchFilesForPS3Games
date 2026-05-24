using System.Text;
using CreateBatchFilesForPS3Games.Services;

namespace CreateBatchFilesForPS3Games.Tests;

public class ParseSfoTests
{
    private static byte[] BuildSfoHeader(int keyTableStart, int dataTableStart, int entryCount)
    {
        var header = new byte[20];
        // PSF magic: 0x00 0x50 0x53 0x46
        BitConverter.GetBytes(0x46535000u).CopyTo(header, 0);
        // Version (0)
        BitConverter.GetBytes(0u).CopyTo(header, 4);
        // Key table start
        BitConverter.GetBytes((uint)keyTableStart).CopyTo(header, 8);
        // Data table start
        BitConverter.GetBytes((uint)dataTableStart).CopyTo(header, 12);
        // Number of entries
        BitConverter.GetBytes((uint)entryCount).CopyTo(header, 16);
        return header;
    }

    private static byte[] BuildSfoEntry(ushort keyOffset, ushort dataFormat, uint dataLength, uint dataOffset)
    {
        var entry = new byte[16];
        BitConverter.GetBytes(keyOffset).CopyTo(entry, 0);
        BitConverter.GetBytes(dataFormat).CopyTo(entry, 2);
        BitConverter.GetBytes(dataLength).CopyTo(entry, 4);
        // dataLengthUsed (bytes 8-11), same as dataLength
        BitConverter.GetBytes(dataLength).CopyTo(entry, 8);
        BitConverter.GetBytes(dataOffset).CopyTo(entry, 12);
        return entry;
    }

    [Fact]
    public void ValidSfo_WithTitleAndTitleId_ReturnsBoth()
    {
        // Build SFO structure:
        // Header: 20 bytes
        // Entry table: 2 entries * 16 = 32 bytes
        // Key table: at offset 52 (20 + 32)
        // Data table: at offset would follow keys

        const int keyTableStart = 20 + 2 * 16; // 52
        var keys = new List<byte>();
        // Key 1: "TITLE" at offset 0
        const int titleOffset = 0;
        keys.AddRange("TITLE\0"u8.ToArray());
        // Key 2: "TITLE_ID" at offset after TITLE
        var titleIdOffset = keys.Count;
        keys.AddRange("TITLE_ID\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        // Data for TITLE
        const int titleDataOffset = 0;
        data.AddRange("Grand Theft Auto V\0"u8.ToArray());
        // Data for TITLE_ID
        var titleIdDataOffset = data.Count;
        data.AddRange("BLES01807\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 2);
        var entry1 = BuildSfoEntry((ushort)titleOffset, 0x0004, 18, (uint)titleDataOffset);
        var entry2 = BuildSfoEntry((ushort)titleIdOffset, 0x0004, 9, (uint)titleIdDataOffset);

        var buffer = new byte[header.Length + entry1.Length + entry2.Length + keys.Count + data.Count];
        var pos = 0;
        header.CopyTo(buffer, pos);
        pos += header.Length;
        entry1.CopyTo(buffer, pos);
        pos += entry1.Length;
        entry2.CopyTo(buffer, pos);
        pos += entry2.Length;
        keys.ToArray().CopyTo(buffer, pos);
        pos += keys.Count;
        data.ToArray().CopyTo(buffer, pos);

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Grand Theft Auto V", result["TITLE"]);
        Assert.Equal("BLES01807", result["TITLE_ID"]);
    }

    [Fact]
    public void ValidSfo_WithOnlyTitle_ReturnsTitle()
    {
        const int keyTableStart = 20 + 16; // 1 entry
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("My Game\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        var entry = BuildSfoEntry(0, 0x0004, 7, 0);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("My Game", result["TITLE"]);
    }

    [Fact]
    public void InvalidMagicNumber_ReturnsNull()
    {
        var buffer = new byte[20];
        BitConverter.GetBytes(0xDEADBEEFu).CopyTo(buffer, 0);

        var result = new SfoParser().ParseSfo(buffer);
        Assert.Null(result);
    }

    [Fact]
    public void BufferTooSmall_ReturnsNull()
    {
        var buffer = new byte[10];

        var result = new SfoParser().ParseSfo(buffer);
        Assert.Null(result);
    }

    [Fact]
    public void EmptyBuffer_ReturnsNull()
    {
        var buffer = Array.Empty<byte>();

        var result = new SfoParser().ParseSfo(buffer);
        Assert.Null(result);
    }

    [Fact]
    public void Sfo_WithIntegerValue_ReturnsStringRepresentation()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("CATEGORY\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        var entry = BuildSfoEntry(0, 0x0404, 4, 0);

        var data = BitConverter.GetBytes(42u);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data);

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("42", result["CATEGORY"]);
    }

    [Fact]
    public void Sfo_WithZeroEntries_ReturnsEmptyDictionary()
    {
        const int keyTableStart = 20;
        const int dataTableStart = 20;

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 0);

        var result = new SfoParser().ParseSfo(header);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sfo_WithDuplicateKeys_UsesFirstValue()
    {
        const int keyTableStart = 20 + 2 * 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("First\0"u8.ToArray());
        data.AddRange("Second\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 2);
        var entry1 = BuildSfoEntry(0, 0x0004, 5, 0);
        var entry2 = BuildSfoEntry(6, 0x0004, 6, 6);

        var buffer = BuildBuffer(header, entry1, entry2, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("First", result["TITLE"]);
    }

    [Fact]
    public void Sfo_WithManyEntries_ReturnsAllUniqueKeys()
    {
        var keys = new List<string>
        {
            "TITLE", "TITLE_ID", "VERSION", "APP_VER", "CATEGORY",
            "PARENTAL_LEVEL", "RESOLUTION", "SOUND_FORMAT"
        };

        var keyTableStart = 20 + keys.Count * 16;
        var keyBytes = new List<byte>();
        var keyOffsets = new List<ushort>();
        foreach (var key in keys)
        {
            keyOffsets.Add((ushort)keyBytes.Count);
            keyBytes.AddRange(Encoding.UTF8.GetBytes(key + "\0"));
        }

        var dataTableStart = keyTableStart + keyBytes.Count;
        var dataBytes = new List<byte>();
        var entries = new List<byte[]>();
        for (var i = 0; i < keys.Count; i++)
        {
            var data = Encoding.UTF8.GetBytes($"Value{i}\0");
            entries.Add(BuildSfoEntry(keyOffsets[i], 0x0004, (uint)(data.Length - 1), (uint)dataBytes.Count));
            dataBytes.AddRange(data);
        }

        var header = BuildSfoHeader(keyTableStart, dataTableStart, keys.Count);

        var buffer = new byte[header.Length + entries.Sum(static e => e.Length) + keyBytes.Count + dataBytes.Count];
        var pos = 0;
        header.CopyTo(buffer, pos);
        pos += header.Length;
        foreach (var entry in entries)
        {
            entry.CopyTo(buffer, pos);
            pos += entry.Length;
        }

        keyBytes.ToArray().CopyTo(buffer, pos);
        pos += keyBytes.Count;
        dataBytes.ToArray().CopyTo(buffer, pos);

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Equal(keys.Count, result.Count);
        for (var i = 0; i < keys.Count; i++)
            Assert.Equal($"Value{i}", result[keys[i]]);
    }

    [Fact]
    public void Sfo_WithStringDataFormatMask_ReturnString()
    {
        // dataFormat & 0xFF == 0x04 means string
        // 0x0204 should also be recognized as string
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        var entry = BuildSfoEntry(0, 0x0204, 7, 0);

        var data = "My Game\0"u8.ToArray();

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data);

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("My Game", result["TITLE"]);
    }

    [Fact]
    public void Sfo_WithNonStringNonIntegerFormat_ReturnsEmptyStringValue()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("UNKNOWN\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // dataFormat 0x0000 is not string and not integer
        var entry = BuildSfoEntry(0, 0x0000, 4, 0);

        var data = BitConverter.GetBytes(1234u);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data);

        var result = new SfoParser().ParseSfo(buffer);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("", result["UNKNOWN"]);
    }

    [Fact]
    public void Sfo_AllZeros_InvalidHeader_ReturnsNull()
    {
        var buffer = new byte[30];

        var result = new SfoParser().ParseSfo(buffer);
        Assert.Null(result);
    }

    [Fact]
    public void Sfo_KeyOffsetBeyondBuffer_SkipsEntry()
    {
        const int keyTableStart = 20 + 16; // 1 entry
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("My Game\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // keyOffset points far beyond the key table
        var entry = BuildSfoEntry(500, 0x0004, 7, 0);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sfo_DataOffsetBeyondBuffer_SkipsEntry()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("My Game\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // dataOffset points far beyond data table
        var entry = BuildSfoEntry(0, 0x0004, 7, 500);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sfo_DataLengthExceedsRemainingBuffer_HandlesGracefully()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("OK\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // dataLength says 1000 bytes, but only 3 bytes of data exist
        var entry = BuildSfoEntry(0, 0x0004, 1000, 0);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("OK", result["TITLE"]);
    }

    [Fact]
    public void Sfo_IntegerDataOffsetPlusFourBeyondBuffer_SkipsEntry()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("CATEGORY\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        var entry = BuildSfoEntry(0, 0x0404, 4, 0);

        var data = BitConverter.GetBytes(42u);

        // Buffer shorter by 1 byte so dataReadOffset+4 exceeds buffer length
        var buffer = new byte[20 + 16 + keys.Count + data.Length - 1];
        var pos = 0;
        header.CopyTo(buffer, pos);
        pos += header.Length;
        entry.CopyTo(buffer, pos);
        pos += entry.Length;
        keys.ToArray().CopyTo(buffer, pos);

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sfo_MaxDataLength_ParsesWithoutCrash()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("OK\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // uint.MaxValue (~4GB) cast to int becomes -1, which ReadNullTerminatedString
        // interprets as "no max length" — should find null terminator gracefully
        var entry = BuildSfoEntry(0, 0x0004, uint.MaxValue, 0);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("OK", result["TITLE"]);
    }

    [Fact]
    public void Sfo_KeyOffsetWithOverflow_SkipsEntry()
    {
        const int keyTableStart = 20 + 16;
        var keys = new List<byte>();
        keys.AddRange("TITLE\0"u8.ToArray());

        var dataTableStart = keyTableStart + keys.Count;

        var data = new List<byte>();
        data.AddRange("My Game\0"u8.ToArray());

        var header = BuildSfoHeader(keyTableStart, dataTableStart, 1);
        // keyOffset = 65530 (near ushort.MaxValue) + keyTableStart overflows int
        var entry = BuildSfoEntry(65530, 0x0004, 7, 0);

        var buffer = BuildBuffer(header, entry, keys.ToArray(), data.ToArray());

        var result = new SfoParser().ParseSfo(buffer);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private static byte[] BuildBuffer(byte[] header, byte[] entry, byte[] keys, byte[] data)
    {
        var buffer = new byte[header.Length + entry.Length + keys.Length + data.Length];
        var pos = 0;
        header.CopyTo(buffer, pos);
        pos += header.Length;
        entry.CopyTo(buffer, pos);
        pos += entry.Length;
        keys.CopyTo(buffer, pos);
        pos += keys.Length;
        data.CopyTo(buffer, pos);
        return buffer;
    }

    private static byte[] BuildBuffer(byte[] header, byte[] entry1, byte[] entry2, byte[] keys, byte[] data)
    {
        var buffer = new byte[header.Length + entry1.Length + entry2.Length + keys.Length + data.Length];
        var pos = 0;
        header.CopyTo(buffer, pos);
        pos += header.Length;
        entry1.CopyTo(buffer, pos);
        pos += entry1.Length;
        entry2.CopyTo(buffer, pos);
        pos += entry2.Length;
        keys.CopyTo(buffer, pos);
        pos += keys.Length;
        data.CopyTo(buffer, pos);
        return buffer;
    }
}