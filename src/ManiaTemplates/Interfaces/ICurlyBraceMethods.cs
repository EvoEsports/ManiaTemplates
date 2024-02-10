using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Interfaces;

public interface ICurlyBraceMethods
{
    protected static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    
    /// <summary>
    /// Takes the contents of double curly braces in a string and wraps them into something else. The second Argument takes a string-argument and returns the newly wrapped string.
    /// </summary>
    public static string ReplaceCurlyBraces(string value, Func<string, string> curlyContentWrapper)
    {
        PreventCurlyBraceCountMismatch(value);
        PreventInterpolationRecursion(value);

        var matches = TemplateInterpolationRegex.Match(value);
        var output = value;

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