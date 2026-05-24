namespace CreateBatchFilesForPS3Games.Tests;

public class IsRomanNumeralTests
{
    [Theory]
    [InlineData("II")]
    [InlineData("III")]
    [InlineData("IV")]
    [InlineData("VI")]
    [InlineData("VII")]
    [InlineData("VIII")]
    [InlineData("IX")]
    [InlineData("XI")]
    [InlineData("XII")]
    [InlineData("XIII")]
    [InlineData("XIV")]
    [InlineData("XV")]
    [InlineData("XVI")]
    [InlineData("XVII")]
    [InlineData("XVIII")]
    [InlineData("XIX")]
    [InlineData("XX")]
    [InlineData("XXI")]
    [InlineData("XXV")]
    [InlineData("XXX")]
    [InlineData("XL")]
    [InlineData("XC")]
    [InlineData("CD")]
    [InlineData("CM")]
    public void ValidRomanNumerals_ReturnTrue(string word)
    {
        Assert.True(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("ii")]
    [InlineData("iii")]
    [InlineData("iv")]
    [InlineData("vi")]
    [InlineData("vii")]
    [InlineData("viii")]
    [InlineData("ix")]
    [InlineData("xi")]
    [InlineData("xii")]
    [InlineData("xiii")]
    [InlineData("xiv")]
    [InlineData("xv")]
    [InlineData("xvi")]
    [InlineData("xvii")]
    [InlineData("xviii")]
    [InlineData("xix")]
    [InlineData("xx")]
    [InlineData("xxi")]
    [InlineData("xxv")]
    [InlineData("xxx")]
    [InlineData("xl")]
    [InlineData("xc")]
    [InlineData("cd")]
    [InlineData("cm")]
    public void ValidRomanNumerals_Lowercase_ReturnTrue(string word)
    {
        Assert.True(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("Ii")]
    [InlineData("Iv")]
    [InlineData("Vi")]
    [InlineData("Xi")]
    public void ValidRomanNumerals_MixedCase_ReturnTrue(string word)
    {
        Assert.True(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("I")]
    [InlineData("V")]
    [InlineData("X")]
    [InlineData("L")]
    [InlineData("C")]
    [InlineData("D")]
    [InlineData("M")]
    public void SingleLetterNumerals_ReturnFalse(string word)
    {
        Assert.False(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("XXI")]
    [InlineData("XXV")]
    [InlineData("XXX")]
    [InlineData("XL")]
    [InlineData("CD")]
    public void RomanNumerals_BeyondXX_ReturnTrue(string word)
    {
        Assert.True(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData(null!)]
    public void EmptyOrWhitespace_ReturnFalse(string? word)
    {
        Assert.False(word != null && new Services.FileNameSanitizer().IsRomanNumeral(word));
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Game")]
    [InlineData("Part2")]
    [InlineData("123")]
    [InlineData("I2")]
    public void NonRomanStrings_ReturnFalse(string word)
    {
        Assert.False(new Services.FileNameSanitizer().IsRomanNumeral(word));
    }
}
