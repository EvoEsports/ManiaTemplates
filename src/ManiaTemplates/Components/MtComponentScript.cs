using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Components;

public class MtComponentScript
{
    public required string Content { get; init; }
    public required bool HasMainMethod { get; init; }
    public required bool Once { get; init; }

    private static readonly Regex DetectMainMethodRegex = new(@"(?s)main\(\).*\{.*\}");
    
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

        if (DetectMainMethodRegex.IsMatch(content))
        {
            main = true;
        }

        return new MtComponentScript
        {
            Content = content,
            HasMainMethod = main,
            Once = once
        };
    }

    public int ContentHash()
    {
        return Content.GetHashCode();
    }
}