using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CreateBatchFilesForPS3Games;

public partial class FileNameSanitizer : IFileNameSanitizer
{
    public string SanitizeFileName(string filename)
    {
        filename = TrialParenthesesPattern().Replace(filename, "");
        filename = TrialWordPattern().Replace(filename, "");
        filename = DemoWordPattern().Replace(filename, "");

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        filename = textInfo.ToTitleCase(filename.ToLowerInvariant());

        var words = filename.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (IsRomanNumeral(words[i]))
            {
                words[i] = words[i].ToUpperInvariant();
            }
        }

        filename = string.Join(" ", words);

        filename = filename.Replace("\u2122", "").Replace("\u00ae", "").Replace(":", " -");

        filename = filename.Trim();

        while (filename.Contains("  ", StringComparison.Ordinal))
        {
            filename = filename.Replace("  ", " ", StringComparison.Ordinal);
        }

        filename = filename.TrimEnd(' ', '.');

        var invalidChars = Path.GetInvalidFileNameChars();
        filename = string.Concat(filename.Split(invalidChars));

        if (string.IsNullOrWhiteSpace(filename))
        {
            filename = "UntitledGame";
        }

        var reservedNames = new[]
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        if (reservedNames.Contains(filename.ToUpperInvariant()))
        {
            filename = $"_{filename}_";
        }

        return filename;
    }

    public bool IsRomanNumeral(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        if (word.Length < 2)
        {
            return false;
        }

        return RomanNumeralPattern().IsMatch(word);
    }

    [GeneratedRegex("^M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$", RegexOptions.IgnoreCase)]
    private static partial Regex RomanNumeralPattern();

    [GeneratedRegex(@"\(Trial\)", RegexOptions.IgnoreCase)]
    private static partial Regex TrialParenthesesPattern();

    [GeneratedRegex(@"\bTrial\b", RegexOptions.IgnoreCase)]
    private static partial Regex TrialWordPattern();

    [GeneratedRegex(@"\bDemo\b", RegexOptions.IgnoreCase)]
    private static partial Regex DemoWordPattern();
}
