namespace CreateBatchFilesForPS3Games.Tests;

public class VerifyWriteAccessTests
{
    [Fact]
    public void WritableDirectory_ReturnsTrue()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            var result = new FileSystemHelper().VerifyWriteAccess(tempPath);
            Assert.True(result);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public void NonExistentDirectory_ReturnsFalse()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}");

        var result = new FileSystemHelper().VerifyWriteAccess(nonExistentPath);
        Assert.False(result);
    }

    [Fact]
    public void EmptyPath_ResolvesToCurrentDirectory()
    {
        var result = new FileSystemHelper().VerifyWriteAccess("");
        Assert.True(result);
    }

    [Fact]
    public void NullPath_ReturnsFalse()
    {
        var result = new FileSystemHelper().VerifyWriteAccess(null!);
        Assert.False(result);
    }

    [Fact]
    public void WritableDirectory_CreatesNoResidualFiles()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        try
        {
            new FileSystemHelper().VerifyWriteAccess(tempPath);
            var files = Directory.GetFiles(tempPath, ".temp_test_*");
            Assert.Empty(files);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }
}
