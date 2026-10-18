using System.Security;
using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

public abstract class MtSpecialCharEscaper
{
    private static Dictionary<string, string> _map = new()
    {
        { "&lt;", "§%lt%§" },
        { "&gt;", "§%gt%§" },
        { "&amp;", "§%amp%§" },
        { "&quot;", "§%quot%§" },
        { "&apos;", "§%apos%§" }
    };

    public static readonly Regex XmlTagFinderRegex = new("<[\\w-]+(?:\\s+[\\w-]+=(?:\"[^\"]*\"|'[^']*'))+\\s*\\/?>");
    public static readonly Regex XmlTagAttributeMatcherDoubleQuote = new("[\\w-]+=\"([^\"]*)\"");
    public static readonly Regex XmlTagAttributeMatcherSingleQuote = new("[\\w-]+='([^']*)'");

    /// <summary>
    /// Takes an XML string and escapes all special chars in node attributes.
    /// </summary>
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
    /// The second argument is a regex to either match ='' or ="" attributes.
    /// </summary>
    public static string FindAndEscapeAttributes(string input, Regex attributeWithQuoteOrApostrophePattern)
    {
        var outputXml = attributeWithQuoteOrApostrophePattern.Replace(SubstituteStrings(input, _map), attributeMatch =>
        {
            //Only escape the attribute value, keeping the attribute name and quotes
            var value = attributeMatch.Groups[1];
            var valueStart = value.Index - attributeMatch.Index;

            return attributeMatch.Value[..valueStart]
                   + SecurityElement.Escape(value.Value)
                   + attributeMatch.Value[(valueStart + value.Length)..];
        });

        return SubstituteStrings(outputXml, FlipMapping(_map));
    }

    /// <summary>
    /// Takes a string and a key/value map.
    /// Replaces all found keys in the string with the value.
    /// </summary>
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
    public static Dictionary<string, string> FlipMapping(Dictionary<string, string> map)
    {
        return map.ToDictionary(x => x.Value, x => x.Key);
    }
}