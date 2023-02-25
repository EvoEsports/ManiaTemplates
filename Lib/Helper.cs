using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ManiaTemplates.Components;

namespace ManiaTemplates.Lib;

public abstract partial class Helper
{
    /// <summary>
    /// Creates a hash from the given string with a fixed length.
    /// </summary>
    internal static string Hash(string input)
    {
        var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
        return string.Concat(hash.Select(b => b.ToString("x2")))[..32];
    }

    /// <summary>
    /// Creates random alpha-numeric string with given length.
    /// </summary>
    public static string RandomString(int length = 16)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Determines whether a XML-node uses one of the given components.
    /// </summary>
    internal static bool UsesComponents(XmlNode node, MtComponentList mtComponents)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            return UsesComponents(child, mtComponents);
        }

        return mtComponents.ContainsKey(node.Name);
    }

    /// <summary>
    /// Takes a XML-string and aligns all nodes properly.
    /// </summary>
    public static string PrettyXml(string? uglyXml = null)
    {
        if (uglyXml == null || uglyXml.Trim().Length == 0)
        {
            return "";
        }

        var stringBuilder = new StringBuilder();
        var element = XElement.Parse(uglyXml);

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        settings.NewLineOnAttributes = false;

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Escape all type-Attributes on property-Nodes in the given XML.
    /// </summary>
    public static string EscapePropertyTypes(string inputXml)
    {
        var outputXml = inputXml;
        var propertyMatcher = ComponentPropertyMatcher();
        var match = propertyMatcher.Match(inputXml);

        while (match.Success)
        {
            var unescapedAttribute = match.Groups[1].Value;
            outputXml = outputXml.Replace(unescapedAttribute, EscapeXmlAttributeString(unescapedAttribute));

            match = match.NextMatch();
        }

        return outputXml;
    }

    /// <summary>
    /// Takes the value of a XML-attribute and escapes special chars, which would break the XML reader.
    /// </summary>
    private static string EscapeXmlAttributeString(string attributeValue)
    {
        return attributeValue.Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;");
    }

    /// <summary>
    /// Takes the escaped value of a XML-attribute and converts it back into it's original form.
    /// </summary>
    public static string ReverseEscapeXmlAttributeString(string attributeValue)
    {
        return attributeValue.Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&");
    }

    [GeneratedRegex("<property.+type=[\"'](.+?)[\"'].+(?:\\s*\\/>|<\\/property>)")]
    private static partial Regex ComponentPropertyMatcher();
}