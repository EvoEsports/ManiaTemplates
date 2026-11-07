using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Lib;

public static class MtEscaper
{
    private static readonly Regex ConsecutiveDashRegex = new("-(?=-)");

    /// <summary>
    /// Converts any given input to an XML safe string.
    /// </summary>
    public static string Escape(dynamic input)
    {
        return SecurityElement.Escape(Convert.ToString(input, CultureInfo.InvariantCulture))
            .Replace("--", "&#45;&#45;");
    }

    /// <summary>
    /// Converts any given input to a string for a ManiaScript string literal, which is rendered inside of XML comments.
    /// Consecutive dashes are split by concatenating strings, as XML comments must not contain --.
    /// </summary>
    public static string EscapeManiaScript(dynamic input)
    {
        string output = Convert.ToString(input, CultureInfo.InvariantCulture) ?? "";

        return ConsecutiveDashRegex.Replace(output.Replace(@"\", @"\\").Replace("\"", "\\\""), "-\"^\"");
    }

    /// <summary>
    /// Converts any given input to a string for raw output in ManiaScript, which is rendered inside of XML comments.
    /// </summary>
    public static string EscapeRawManiaScript(dynamic input)
    {
        string output = Convert.ToString(input, CultureInfo.InvariantCulture) ?? "";

        if (output.Contains("--"))
        {
            throw new InvalidManiaScriptException($"Raw ManiaScript output must not contain '--': {output}");
        }

        return output;
    }
}