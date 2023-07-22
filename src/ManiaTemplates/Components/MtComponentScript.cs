using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Components;

public class MtComponentScript
{
    public required string Content { get; init; }
    public required bool HasMainMethod { get; init; }
    public required bool Once { get; init; }
    public int Depth { get; set; }

    private static readonly Regex DetectMainMethodRegex = new(@"(?s)main\(\).*\{.*\}");
    
    /// <summary>
    /// Creates a MtComponentScript instance from a components script-node.
    /// </summary>
    public static MtComponentScript FromNode(ManiaTemplateEngine engine, XmlNode node)
    {
        string? content = null;
        var once = false;

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

        return new MtComponentScript
        {
            Content = content,
            HasMainMethod = DetectMainMethodRegex.IsMatch(content),
            Once = once
        };
    }

    public string ContentHash()
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Content)));
    }
}