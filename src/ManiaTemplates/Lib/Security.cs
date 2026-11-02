using System.Globalization;
using System.Security;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Lib;

public abstract class Security
{
    /// <summary>
    /// Converts any given input to an XML safe string.
    /// </summary>
    public static string Escape(dynamic input)
    {
        return SecurityElement.Escape(Convert.ToString(input, CultureInfo.InvariantCulture))
            .Replace("--", "&#45;&#45;");
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