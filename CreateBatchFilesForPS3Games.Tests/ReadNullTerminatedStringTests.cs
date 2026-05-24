namespace CreateBatchFilesForPS3Games.Tests;

public class ReadNullTerminatedStringTests
{
    [Fact]
    public void SimpleString_ReturnsCorrectValue()
    {
        var bytes = "Hello World\0Extra"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void StringAtOffset_ReturnsCorrectValue()
    {
        var bytes = "ABCDHello World\0Extra"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 4);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void WithMaxLength_TruncatesToMaxLength()
    {
        var bytes = "Hello World1234567890\0"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0, 11);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void WithMaxLength_LessThanNullTerminator_Truncates()
    {
        var bytes = "Hello\0World"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0, 3);
        Assert.Equal("Hel", result);
    }

    [Fact]
    public void NoNullTerminator_UsesBufferEnd()
    {
        var bytes = "Hello World"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void NullAtStart_ReturnsEmptyString()
    {
        var bytes = "\0Hello World"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0);
        Assert.Equal("", result);
    }

    [Fact]
    public void NullAtOffset_ReturnsEmptyString()
    {
        var bytes = "ABCD\0Extra"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 4);
        Assert.Equal("", result);
    }

    [Fact]
    public void OffsetAtEndOfBuffer_ReturnsEmptyString()
    {
        var bytes = "Hello"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 5);
        Assert.Equal("", result);
    }

    [Fact]
    public void OffsetBeyondEndOfBuffer_ReturnsEmptyString()
    {
        var bytes = "Hello"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 10);
        Assert.Equal("", result);
    }

    [Fact]
    public void EmptyBuffer_ReturnsEmptyString()
    {
        var bytes = Array.Empty<byte>();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0);
        Assert.Equal("", result);
    }

    [Fact]
    public void MaxLengthZero_ReturnsEmptyString()
    {
        var bytes = "Hello\0World"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0, 0);
        Assert.Equal("", result);
    }

    [Fact]
    public void NegativeMaxLength_IgnoresMaxLength()
    {
        var bytes = "Hello\0World"u8.ToArray();
        var result = new SfoParser().ReadNullTerminatedString(bytes, 0);
        Assert.Equal("Hello", result);
    }
}
