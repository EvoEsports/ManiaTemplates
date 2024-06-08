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
        return SecurityElement.Escape(Convert.ToString(input, CultureInfo.InvariantCulture))
            .Replace("--", "&#45;&#45;");
    }
}