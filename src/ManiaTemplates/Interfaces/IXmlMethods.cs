using System.Xml;
using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

public interface IXmlMethods: ICurlyBraceMethods
{
    /// <summary>
    /// Parses the attributes of a XmlNode to an MtComponentAttributes-instance.
    /// </summary>
    public static MtComponentAttributes GetAttributes(XmlNode node)
    {
        var attributeList = new MtComponentAttributes();
        if (node.Attributes == null) return attributeList;

        foreach (XmlAttribute attribute in node.Attributes)
        {
            attributeList.Add(attribute.Name, attribute.Value);
        }

        return attributeList;
    }
    
    /// <summary>
    /// Creates a xml opening tag for the given string and attribute list.
    /// </summary>
    public static string CreateOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren, Func<string, string> curlyContentWrapper)
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
    public static string CreateClosingTag(string tag)
    {
        return $"</{tag}>";
    }

    /// <summary>
    /// Converts any valid XML-string into an XmlNode-element.
    /// </summary>
    public static XmlNode NodeFromString(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }
}