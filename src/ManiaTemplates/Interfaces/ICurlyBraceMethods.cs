using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Interfaces;

public interface ICurlyBraceMethods
{
    protected static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    protected static readonly Regex TemplateRawInterpolationRegex = new(@"\{!\s*(.+?)\s*!\}");

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
        var output = value;
        output = ReplaceCurlyBraces(output, curlyRawContentWrapper, TemplateRawInterpolationRegex);
        output = ReplaceCurlyBraces(output, curlyContentWrapper);

        return output;
    }

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

        var output = value;
        var pattern = interpolationPattern ?? TemplateInterpolationRegex;
        var matches = pattern.Match(value);

        while (matches.Success)
        {
            var match = matches.Groups[0].Value.Trim();
            var content = matches.Groups[1].Value.Trim();

            output = output.Replace(match, curlyContentWrapper(content));

            matches = matches.NextMatch();
        }

        return output;
    }

    /// <summary>
    /// Checks whether double interpolation exists ({{ {{ a }} {{ b }} }}) and throws exception if so.
    /// </summary>
    public static void PreventInterpolationRecursion(string value)
    {
        //TODO: find proper algorithm
        // var openCurlyBraces = 0;
        // foreach (var character in value.ToCharArray())
        // {
        //     if (character == '{')
        //     {
        //         openCurlyBraces++;
        //
        //         if (openCurlyBraces >= 4)
        //         {
        //             throw new InterpolationRecursionException(
        //                 $"Double interpolation found in: {value}. You must not use double curly braces inside other double curly braces.");
        //         }
        //     }
        //     else if (character == '}')
        //     {
        //         openCurlyBraces--;
        //     }
        // }
    }

    /// <summary>
    /// Checks whether double interpolation exists ({{ {{ a }} {{ b }} }}) and throws exception if so.
    /// </summary>
    public static void PreventCurlyBraceCountMismatch(string value)
    {
        var openCurlyBraces = 0;
        foreach (var character in value.ToCharArray())
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