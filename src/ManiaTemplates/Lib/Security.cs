using System.Globalization;
using System.Security;

namespace ManiaTemplates.Lib;

public abstract class Security
{
    /// <summary>
    /// Converts any given input to an XML safe string.
    /// </summary>
    public static string Escape(dynamic input)
    {
        return EscapeDoubleMinus(SecurityElement.Escape(Convert.ToString(input, CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Replaces -- in strings with their ASCII escape code.
    /// Meant to be used for strings inside XML comments.
    /// </summary>
    public static string EscapeDoubleMinus(string input)
    {
        return input;
        // return input.Replace("--", "&#45;&#45;");
    }
}