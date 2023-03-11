using System;
using System.Xml;

namespace ManiaTemplates.Components;

public class MtComponentScript
{
    public required string Content { get; init; }
    public required bool Main { get; init; }
    public required bool Once { get; init; }

    /// <summary>
    /// Creates a MtComponentScript instance from a components script-node.
    /// </summary>
    public static MtComponentScript FromNode(XmlNode node)
    {
        string? resource = null, content = null;
        bool main = false, once = false;

        if (node.InnerText.Length > 0)
        {
            content = node.InnerText;
        }

        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                switch (attribute.Name)
                {
                    case "resource":
                        resource = attribute.Value;
                        //TODO: load contents from resource
                        break;

                    case "main":
                        main = true;
                        break;

                    case "once":
                        once = true;
                        break;
                }
            }
        }

        if (content == null)
        {
            throw new Exception(
                "Failed to get ManiaScript contents. Script tags need to either specify a body or resource-attribute.");
        }

        return new MtComponentScript
        {
            Content = content,
            Main = main,
            Once = once
        };
    }

    public int ContentHash()
    {
        return Content.GetHashCode();
    }
}
