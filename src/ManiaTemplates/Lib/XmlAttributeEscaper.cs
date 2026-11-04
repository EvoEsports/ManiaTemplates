using System.Security;
using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

/// <summary>
/// Escapes special characters in attribute values, so templates with them can be parsed as XML.
/// </summary>
internal static class XmlAttributeEscaper
{
    // Entities already in the template are replaced by placeholders, so they are not escaped twice
    private static readonly Dictionary<string, string> EntityPlaceholders = new()
    {
        { "&lt;", "§%lt%§" },
        { "&gt;", "§%gt%§" },
        { "&amp;", "§%amp%§" },
        { "&quot;", "§%quot%§" },
        { "&apos;", "§%apos%§" }
    };

    private static readonly Dictionary<string, string> PlaceholderEntities = FlipMapping(EntityPlaceholders);

    /// <summary>
    /// Matches opening tags with attributes.
    /// </summary>
    public static readonly Regex XmlTagFinderRegex = new("<[\\w-]+(?:\\s+[\\w-]+=(?:\"[^\"]*\"|'[^']*'))+\\s*\\/?>");

    /// <summary>
    /// Matches attributes with values in double quotes.
    /// </summary>
    public static readonly Regex XmlTagAttributeMatcherDoubleQuote = new("[\\w-]+=\"([^\"]*)\"");

    /// <summary>
    /// Matches attributes with values in single quotes.
    /// </summary>
    public static readonly Regex XmlTagAttributeMatcherSingleQuote = new("[\\w-]+='([^']*)'");

    /// <summary>
    /// Takes an XML string and escapes all special chars in node attributes.
    /// </summary>
    /// <param name="inputXmlString">The XML string.</param>
    public static string EscapeXmlSpecialCharsInAttributes(string inputXmlString)
    {
        return XmlTagFinderRegex.Replace(inputXmlString, tagMatch =>
        {
            var escapedXmlTag = FindAndEscapeAttributes(tagMatch.Value, XmlTagAttributeMatcherDoubleQuote);
            return FindAndEscapeAttributes(escapedXmlTag, XmlTagAttributeMatcherSingleQuote);
        });
    }

    /// <summary>
    /// Takes the string of a matched XML tag and escapes the attribute values.
    /// </summary>
    /// <param name="input">The XML tag.</param>
    /// <param name="attributeWithQuoteOrApostrophePattern">The pattern matching attributes with values in single or double quotes.</param>
    public static string FindAndEscapeAttributes(string input, Regex attributeWithQuoteOrApostrophePattern)
    {
        var outputXml = attributeWithQuoteOrApostrophePattern.Replace(SubstituteStrings(input, EntityPlaceholders), attributeMatch =>
        {
            // Only escape the attribute value, keeping the attribute name and quotes
            var value = attributeMatch.Groups[1];
            var valueStart = value.Index - attributeMatch.Index;

            return attributeMatch.Value[..valueStart]
                   + SecurityElement.Escape(value.Value)
                   + attributeMatch.Value[(valueStart + value.Length)..];
        });

        return SubstituteStrings(outputXml, PlaceholderEntities);
    }

    /// <summary>
    /// Takes a string and a key/value map.
    /// Replaces all found keys in the string with the value.
    /// </summary>
    /// <param name="input">The string to replace in.</param>
    /// <param name="map">The strings to replace, mapped to their replacements.</param>
    public static string SubstituteStrings(string input, Dictionary<string, string> map)
    {
        var output = input;
        foreach (var (escapeSequence, substitute) in map)
        {
            output = output.Replace(escapeSequence, substitute);
        }

        return output;
    }

    /// <summary>
    /// Switches keys with values in the given dictionary and returns a new one.
    /// </summary>
    /// <param name="map">The dictionary to flip.</param>
    public static Dictionary<string, string> FlipMapping(Dictionary<string, string> map)
    {
        return map.ToDictionary(x => x.Value, x => x.Key);
    }
}