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

    public static readonly Regex XmlTagFinderRegex = new("<[\\w-]+(?:\\s+[\\w-]+=[\"'].+?[\"'])+\\s*\\/?>");
    public static readonly Regex XmlTagAttributeMatcherDoubleQuote = new("[\\w-]+=\"(.+?)\"");
    public static readonly Regex XmlTagAttributeMatcherSingleQuote = new("[\\w-]+='(.+?)'");

    /// <summary>
    /// Takes an XML string and escapes all special chars in node attributes.
    /// </summary>
    public static string EscapeXmlSpecialCharsInAttributes(string inputXmlString)
    {
        var outputXml = inputXmlString;
        var xmlTagMatcher = XmlTagFinderRegex;
        var tagMatch = xmlTagMatcher.Match(inputXmlString);

        while (tagMatch.Success)
        {
            var unescapedXmlTag = tagMatch.Value;
            var escapedXmlTag =
                FindAndEscapeAttributes(unescapedXmlTag, XmlTagAttributeMatcherDoubleQuote);
            escapedXmlTag = FindAndEscapeAttributes(escapedXmlTag, XmlTagAttributeMatcherSingleQuote);

            outputXml = outputXml.Replace(unescapedXmlTag, escapedXmlTag);
            tagMatch = tagMatch.NextMatch();
        }

        return outputXml;
    }

    /// <summary>
    /// Takes the string of a matched XML tag and escapes the attribute values.
    /// The second argument is a regex to either match ='' or ="" attributes.
    /// </summary>
    public static string FindAndEscapeAttributes(string input, Regex attributeWithQuoteOrApostrophePattern)
    {
        var outputXml = SubstituteStrings(input, _map);
        var attributeMatch = attributeWithQuoteOrApostrophePattern.Match(outputXml);

        while (attributeMatch.Success)
        {
            var unescapedAttributeValue = attributeMatch.Groups[1].Value;
            var escapedAttributeValue = SecurityElement.Escape(unescapedAttributeValue);
            outputXml = outputXml.Replace(unescapedAttributeValue, escapedAttributeValue);

            attributeMatch = attributeMatch.NextMatch();
        }

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