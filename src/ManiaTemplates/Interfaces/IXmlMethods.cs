using System.Xml;
using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

public interface IXmlMethods: ICurlyBraceMethods
{
    /// <summary>
    /// Creates a xml opening tag for the given string and attribute list.
    /// </summary>
    public static string CreateXmlOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren, Func<string, string> curlyContentWrapper)
    {
        var output = $"<{tag}";

        foreach (var (attributeName, attributeValue) in attributeList)
        {
            output += @$" {attributeName}=""{ReplaceCurlyBraces(attributeValue, curlyContentWrapper)}""";
        }

        if (!hasChildren)
        {
            output += " /";
        }

        return output + ">";
    }

    /// <summary>
    /// Creates a xml closing tag for the given string.
    /// </summary>
    public static string CreateXmlClosingTag(string tag)
    {
        return $"</{tag}>";
    }

    /// <summary>
    /// Converts any valid XML-string into an XmlNode-element.
    /// </summary>
    public static XmlNode XmlStringToNode(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }
}