using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Lib;

internal static class CurlyBraceUtils
{
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    private static readonly Regex TemplateRawInterpolationRegex = new(@"\{!\s*(.+?)\s*!\}");

    /// <summary>
    /// Takes the contents of double curly braces in a string and wraps them into something else.
    /// The second argument is a function that takes a string-argument and returns the newly wrapped string.
    /// The third argument is a function that takes a string-argument and returns the newly wrapped (escaped) string.
    /// </summary>
    public static string ReplaceCurlyBracesWithRawOutput(
        string value,
        Func<string, string> curlyContentWrapper,
        Func<string, string> curlyRawContentWrapper
    )
    {
        var output = ReplaceCurlyBraces(value, curlyRawContentWrapper, TemplateRawInterpolationRegex);
        return ReplaceCurlyBraces(output, curlyContentWrapper);
    }

    /// <summary>
    /// Removes the double curly braces around expressions, like in conditions written as if="{{ expression }}".
    /// </summary>
    public static string RemoveCurlyBraces(string value) => TemplateInterpolationRegex.Replace(value, "$1");

    /// <summary>
    /// Takes the contents of double curly braces in a string and wraps them into something else.
    /// The second argument takes a string-argument and returns the newly wrapped string.
    /// The third argument is an optional regex pattern to find substrings to replace.
    /// </summary>
    public static string ReplaceCurlyBraces(string value, Func<string, string> curlyContentWrapper,
        Regex? interpolationPattern = null)
    {
        PreventCurlyBraceCountMismatch(value);
        PreventInterpolationRecursion(value);

        return (interpolationPattern ?? TemplateInterpolationRegex)
            .Replace(value, match => curlyContentWrapper(match.Groups[1].Value.Trim()));
    }

    /// <summary>
    /// Checks whether double interpolation exists ({{ {{ a }} {{ b }} }}) and throws exception if so.
    /// </summary>
    public static void PreventInterpolationRecursion(string value)
    {
        var start = value.IndexOf("{{", StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = value.IndexOf("}}", start + 2, StringComparison.Ordinal);
            var nextStart = value.IndexOf("{{", start + 2, StringComparison.Ordinal);

            if (nextStart >= 0 && (end < 0 || nextStart < end))
            {
                throw new InterpolationRecursionException(
                    $"Double interpolation found in: {value}. You must not use double curly braces inside other double curly braces.");
            }

            start = end < 0 ? -1 : value.IndexOf("{{", end + 2, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Checks whether opening and closing curly braces are balanced and throws exception if not.
    /// </summary>
    public static void PreventCurlyBraceCountMismatch(string value)
    {
        var openCurlyBraces = 0;
        foreach (var character in value)
        {
            if (character == '{')
            {
                openCurlyBraces++;
            }
            else if (character == '}')
            {
                openCurlyBraces--;
            }
        }

        if (openCurlyBraces != 0)
        {
            throw new CurlyBraceCountMismatchException($"Found curly brace count mismatch in: {value}.");
        }
    }
}