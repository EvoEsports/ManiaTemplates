using System.Text;
using System.Xml;
using ManiaTemplates.Components;

namespace ManiaTemplates.Lib;

/// <summary>
/// Helpers for parsing XML nodes and creating XML tags.
/// </summary>
internal static class XmlUtils
{
    /// <summary>
    /// Parses the attributes of a XmlNode to an MtComponentAttributes-instance.
    /// </summary>
    /// <param name="node">The node.</param>
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
    /// <param name="tag">The name of the tag.</param>
    /// <param name="attributeList">The attributes rendered on the tag.</param>
    /// <param name="hasChildren">Whether the tag has children, else it is self-closing.</param>
    /// <param name="curlyContentWrapper">Wraps the expressions of interpolations in attribute values.</param>
    public static string CreateOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren,
        Func<string, string> curlyContentWrapper)
    {
        var tagParts = new List<string>
        {
            $"<{tag}"
        };

        foreach (var (attributeName, attribute) in attributeList)
        {
            var newAttribute = @$"{attributeName}=""{CurlyBraceUtils.ReplaceCurlyBraces(attribute.Value, curlyContentWrapper)}""";

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
    /// <param name="tag">The name of the tag.</param>
    public static string CreateClosingTag(string tag)
    {
        return $"</{tag}>";
    }

    /// <summary>
    /// Converts any valid XML-string into an XmlNode-element.
    /// </summary>
    /// <param name="content">The XML string.</param>
    public static XmlNode NodeFromString(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }
}