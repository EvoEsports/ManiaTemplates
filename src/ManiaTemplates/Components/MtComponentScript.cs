using System.Xml;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Components;

public class MtComponentScript
{
    public required string Content { get; init; }
    public required bool Main { get; init; }
    public required bool Once { get; init; }

    /// <summary>
    /// Creates a MtComponentScript instance from a components script-node.
    /// </summary>
    public static MtComponentScript FromNode(ManiaTemplateEngine engine, XmlNode node)
    {
        string? content = null;
        bool main = false, once = false;

        if (node.InnerXml.Length > 0)
        {
            content = node.InnerXml;
        }

        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                switch (attribute.Name)
                {
                    case "resource":
                        content = engine.GetManiaScript(attribute.Value);
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
            throw new ManiaScriptSourceMissingException(
                "Script tags need to either specify a body or resource-attribute.");
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