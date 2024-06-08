using System.Text;
using System.Xml;
using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

public interface IXmlMethods : ICurlyBraceMethods
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
            attributeList.Add(attribute.Name, new MtComponentAttribute
            {
                Value = attribute.Value
            });
        }

        return attributeList;
    }

    /// <summary>
    /// Creates a xml opening tag for the given string and attribute list.
    /// </summary>
    public static string CreateOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren,
        Func<string, string> curlyContentWrapper)
    {
        var tagParts = new List<string>
        {
            $"<{tag}"
        };

        foreach (var (attributeName, attribute) in attributeList)
        {
            var newAttribute = @$"{attributeName}=""{ReplaceCurlyBraces(attribute.Value, curlyContentWrapper)}""";

            if (!attribute.IsFallthroughAttribute)
            {
                tagParts.Add(newAttribute);
                continue;
            }

            tagParts.Add(new StringBuilder()
                .Append($"<#+ if({attribute.Alias} != null){{ #>")
                .Append(newAttribute)
                .Append("<#+ } #>")
                .ToString());
        }

        if (!hasChildren)
        {
            tagParts.Add("/");
        }
        
        return string.Join(" ", tagParts) + ">";
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