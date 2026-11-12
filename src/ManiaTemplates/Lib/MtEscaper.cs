using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Lib;

/// <summary>
/// Escapes values written to the output, called by the generated templates.
/// </summary>
public static class MtEscaper
{
    private static readonly Regex ConsecutiveDashRegex = new("-(?=-)");

    /// <summary>
    /// Converts any given input to an XML safe string.
    /// </summary>
    /// <param name="input">The value to write.</param>
    public static string Escape(object? input)
    {
        // Not dynamic, as the runtime binder caches the types it is called with, which would keep collectible load
        // contexts alive
        return SecurityElement.Escape(Convert.ToString(input, CultureInfo.InvariantCulture) ?? "")
            .Replace("--", "&#45;&#45;");
    }

    /// <summary>
    /// Converts any given input to a string for a ManiaScript string literal, which is rendered inside of XML comments.
    /// Consecutive dashes are split by concatenating strings, as XML comments must not contain --.
    /// </summary>
    /// <param name="input">The value to write.</param>
    public static string EscapeManiaScript(object? input)
    {
        var output = Convert.ToString(input, CultureInfo.InvariantCulture) ?? "";

        return ConsecutiveDashRegex.Replace(output.Replace(@"\", @"\\").Replace("\"", "\\\""), "-\"^\"");
    }

    /// <summary>
    /// Converts any given input to a string for raw output in ManiaScript, which is rendered inside of XML comments.
    /// </summary>
    /// <param name="input">The value to write.</param>
    public static string EscapeRawManiaScript(object? input)
    {
        var output = Convert.ToString(input, CultureInfo.InvariantCulture) ?? "";

        if (output.Contains("--"))
        {
            throw new InvalidManiaScriptException($"Raw ManiaScript output must not contain '--': {output}");
        }

        return output;
    }
}