using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Lib;

/// <summary>
/// Helpers for interpolations with {{ }} and raw interpolations with {! !}.
/// </summary>
internal static class CurlyBraceUtils
{
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    private static readonly Regex TemplateRawInterpolationRegex = new(@"\{!\s*(.+?)\s*!\}");

    /// <summary>
    /// Replaces the interpolations with {{ }} and the raw interpolations with {! !} in a string with the wrapped expressions.
    /// </summary>
    /// <param name="value">The string containing interpolations.</param>
    /// <param name="curlyContentWrapper">Wraps the expression of a {{ }} interpolation, escaping its output.</param>
    /// <param name="curlyRawContentWrapper">Wraps the expression of a {! !} interpolation, writing its output unescaped.</param>
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
    /// <param name="value">The string containing interpolations.</param>
    public static string RemoveCurlyBraces(string value) => TemplateInterpolationRegex.Replace(value, "$1");

    /// <summary>
    /// Replaces the interpolations in a string with the wrapped expressions.
    /// </summary>
    /// <param name="value">The string containing interpolations.</param>
    /// <param name="curlyContentWrapper">Wraps the expression of an interpolation.</param>
    /// <param name="interpolationPattern">The pattern finding the interpolations, {{ }} if null.</param>
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
    /// <param name="value">The string containing interpolations.</param>
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
    /// <param name="value">The string containing interpolations.</param>
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