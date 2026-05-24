namespace CreateBatchFilesForPS3Games.Tests;

public class SanitizeFileNameTests
{
    [Fact]
    public void NormalTitle_ReturnsTitleCase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Grand Theft Auto V");
        Assert.Equal("Grand Theft Auto V", result);
    }

    [Fact]
    public void AllCapsTitle_ReturnsTitleCase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("GRAND THEFT AUTO V");
        Assert.Equal("Grand Theft Auto V", result);
    }

    [Fact]
    public void MixedCaseTitle_ReturnsTitleCase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("gRaNd tHeFt AuTo V");
        Assert.Equal("Grand Theft Auto V", result);
    }

    [Fact]
    public void RemovesTrialParentheses_CaseInsensitive()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game Name (Trial)");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void RemovesTrial_CaseInsensitive()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game Name Trial");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void RemovesTrial_Lowercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("game name trial");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void RemovesDemo_CaseInsensitive()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game Name Demo");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void RemovesDemo_Lowercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("game name demo");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void TrialInMiddleOfWord_ShouldNotBeRemoved()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Industrial Game");
        Assert.Equal("Industrial Game", result);
    }

    [Fact]
    public void DemoInMiddleOfWord_ShouldNotBeRemoved()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Demolition Derby");
        Assert.Equal("Demolition Derby", result);
    }

    [Fact]
    public void OnlyTrialText_ReturnsUntitledGame()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Trial");
        Assert.Equal("UntitledGame", result);
    }

    [Fact]
    public void OnlyDemoText_ReturnsUntitledGame()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Demo");
        Assert.Equal("UntitledGame", result);
    }

    [Fact]
    public void RomanNumeralIi_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Ii");
        Assert.Equal("Final Fantasy II", result);
    }

    [Fact]
    public void RomanNumeralIii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Iii");
        Assert.Equal("Final Fantasy III", result);
    }

    [Fact]
    public void RomanNumeralIv_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Iv");
        Assert.Equal("Final Fantasy IV", result);
    }

    [Fact]
    public void RomanNumeralVi_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Vi");
        Assert.Equal("Final Fantasy VI", result);
    }

    [Fact]
    public void RomanNumeralVii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Vii");
        Assert.Equal("Final Fantasy VII", result);
    }

    [Fact]
    public void RomanNumeralViii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Viii");
        Assert.Equal("Final Fantasy VIII", result);
    }

    [Fact]
    public void RomanNumeralIx_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Ix");
        Assert.Equal("Final Fantasy IX", result);
    }

    [Fact]
    public void RomanNumeralX_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy X");
        Assert.Equal("Final Fantasy X", result);
    }

    [Fact]
    public void RomanNumeralXi_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xi");
        Assert.Equal("Final Fantasy XI", result);
    }

    [Fact]
    public void RomanNumeralXii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xii");
        Assert.Equal("Final Fantasy XII", result);
    }

    [Fact]
    public void RomanNumeralXiii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xiii");
        Assert.Equal("Final Fantasy XIII", result);
    }

    [Fact]
    public void RomanNumeralXiv_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xiv");
        Assert.Equal("Final Fantasy XIV", result);
    }

    [Fact]
    public void RomanNumeralXv_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xv");
        Assert.Equal("Final Fantasy XV", result);
    }

    [Fact]
    public void RomanNumeralXvi_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xvi");
        Assert.Equal("Final Fantasy XVI", result);
    }

    [Fact]
    public void RomanNumeralXvii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xvii");
        Assert.Equal("Final Fantasy XVII", result);
    }

    [Fact]
    public void RomanNumeralXviii_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xviii");
        Assert.Equal("Final Fantasy XVIII", result);
    }

    [Fact]
    public void RomanNumeralXix_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xix");
        Assert.Equal("Final Fantasy XIX", result);
    }

    [Fact]
    public void RomanNumeralXx_BecomesUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Final Fantasy Xx");
        Assert.Equal("Final Fantasy XX", result);
    }

    [Fact]
    public void RemovesTrademarkSymbol()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game\u2122");
        Assert.Equal("Game", result);
    }

    [Fact]
    public void RemovesCopyrightSymbol()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game\u00ae");
        Assert.Equal("Game", result);
    }

    [Fact]
    public void Colon_ReplacedWithDash()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game: Subtitle");
        Assert.Equal("Game - Subtitle", result);
    }

    [Fact]
    public void MultipleSpaces_Collapsed()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game   With   Spaces");
        Assert.Equal("Game With Spaces", result);
    }

    [Fact]
    public void TrailingSpacesAndDots_Removed()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game Name . ..");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void LeadingAndTrailingWhitespace_Trimmed()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("  Game Name  ");
        Assert.Equal("Game Name", result);
    }

    [Fact]
    public void WindowsReservedNameCon_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("CON");
        Assert.Equal("_Con_", result);
    }

    [Fact]
    public void WindowsReservedNamePrn_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("PRN");
        Assert.Equal("_Prn_", result);
    }

    [Fact]
    public void WindowsReservedNameAux_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("AUX");
        Assert.Equal("_Aux_", result);
    }

    [Fact]
    public void WindowsReservedNameNul_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("NUL");
        Assert.Equal("_Nul_", result);
    }

    [Fact]
    public void WindowsReservedNameCom1_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("COM1");
        Assert.Equal("_Com1_", result);
    }

    [Fact]
    public void WindowsReservedNameCom9_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("COM9");
        Assert.Equal("_Com9_", result);
    }

    [Fact]
    public void WindowsReservedNameLpt1_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("LPT1");
        Assert.Equal("_Lpt1_", result);
    }

    [Fact]
    public void WindowsReservedNameLpt9_ReturnsWrappedWithUnderscores()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("LPT9");
        Assert.Equal("_Lpt9_", result);
    }

    [Fact]
    public void Com10_NotReserved_NotWrapped()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("COM10");
        Assert.Equal("Com10", result);
    }

    [Fact]
    public void InvalidFileNameCharacters_Removed()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game<Name>With\"Invalid*/\\|?:Chars");
        Assert.Equal("GameNameWithInvalid -Chars", result);
    }

    [Fact]
    public void NullCharacter_Removed()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("Game\0Name");
        Assert.Equal("GameName", result);
    }

    [Fact]
    public void EmptyString_ReturnsUntitledGame()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("");
        Assert.Equal("UntitledGame", result);
    }

    [Fact]
    public void WhitespaceString_ReturnsUntitledGame()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("   ");
        Assert.Equal("UntitledGame", result);
    }

    [Fact]
    public void AllInvalidChars_ReturnsUntitledGame()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("???");
        Assert.Equal("UntitledGame", result);
    }

    [Fact]
    public void TrialInParentheses_MultipleOccurrences()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("(Trial) Game (Trial)");
        Assert.Equal("Game", result);
    }

    [Fact]
    public void SingleCharacterTitle_ReturnsUppercase()
    {
        var result = new Services.FileNameSanitizer().SanitizeFileName("a");
        Assert.Equal("A", result);
    }
}
