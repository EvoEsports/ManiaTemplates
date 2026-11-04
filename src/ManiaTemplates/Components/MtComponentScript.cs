using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Components;

/// <summary>
/// A ManiaScript of a component, defined inline or loaded from a registered ManiaScript resource.
/// </summary>
public class MtComponentScript
{
    /// <summary>
    /// The ManiaScript code.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Whether the script defines a main method, which is only allowed once per manialink.
    /// </summary>
    public required bool HasMainMethod { get; init; }

    /// <summary>
    /// Whether the script is only rendered once, even if the component is used multiple times.
    /// </summary>
    public required bool Once { get; init; }

    /// <summary>
    /// The nesting depth of the component using the script, which is currently not set.
    /// </summary>
    public int Depth { get; set; }

    private static readonly Regex DetectMainMethodRegex = new(@"(?s)main\(\).*\{.*\}");
    
    /// <summary>
    /// Creates a MtComponentScript instance from a components script-node.
    /// </summary>
    /// <param name="engine">The engine providing the ManiaScript of the resource attribute.</param>
    /// <param name="node">The script node.</param>
    public static MtComponentScript FromNode(ManiaTemplateEngine engine, XmlNode node)
    {
        string? content = null;
        var once = false;

        if (node.HasChildNodes)
        {
            content = GetInlineContent(node) ?? throw new InvalidManiaScriptException(
                $"Script tags must only contain ManiaScript, found other nodes in: {node.InnerXml}");
        }

        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                switch (attribute.Name)
                {
                    case "resource":
                        content = GetResourceContent(engine.GetManiaScript(attribute.Value));
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

        if (content.Contains("--"))
        {
            throw new InvalidManiaScriptException(
                $"ManiaScript must not contain '--', as scripts are rendered inside of XML comments: {content}");
        }

        return new MtComponentScript
        {
            Content = content,
            HasMainMethod = DetectMainMethodRegex.IsMatch(content),
            Once = once
        };
    }

    /// <summary>
    /// Gets the ManiaScript of a script-node, which can be written as text, inside of XML comments or CDATA sections.
    /// Returns null if the node contains anything else.
    /// </summary>
    private static string? GetInlineContent(XmlNode node)
    {
        var content = new StringBuilder();

        foreach (XmlNode childNode in node.ChildNodes)
        {
            // Text, CDATA, comments and whitespace
            if (childNode is not XmlCharacterData)
            {
                return null;
            }

            content.Append(childNode.Value);
        }

        return content.ToString();
    }

    /// <summary>
    /// Gets the ManiaScript of a resource. Resources used to be inserted into a script-node as they are, so they are
    /// read the same way: comments, CDATA sections and additional script-nodes are resolved if the resource is valid
    /// XML, otherwise it is used as plain ManiaScript.
    /// </summary>
    private static string GetResourceContent(string resource)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml($"<resource><script>{resource.Trim('\uFEFF')}</script></resource>");
        }
        catch (XmlException)
        {
            return resource;
        }

        var scripts = new List<string>();
        foreach (XmlNode scriptNode in document.DocumentElement!.ChildNodes)
        {
            if (scriptNode.Name != "script")
            {
                continue;
            }

            var content = GetInlineContent(scriptNode);
            if (content == null)
            {
                return resource;
            }

            scripts.Add(content);
        }

        return string.Join("\n", scripts);
    }

    /// <summary>
    /// Returns a hash of the script, used to detect the same script used multiple times.
    /// </summary>
    public string ContentHash()
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Content)));
    }
}