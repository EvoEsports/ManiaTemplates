using System.Diagnostics;
using System.Reflection;
using System.Xml;
using ManiaTemplates.Interfaces;
using ManiaTemplates.Lib;
using ManiaTemplates.TemplateSources;

namespace ManiaTemplates.Components;

public class MtComponent
{
    public required string Tag { get; set; }
    public required string TemplateContent { get; init; }
    public required IMtTemplate TemplateFileFile { get; init; }
    public required bool HasSlot { get; init; }
    public required MtComponentMap ImportedMtComponents { get; init; }
    public required Dictionary<string, MtComponentProperty> Properties { get; init; }
    public required List<string> Namespaces { get; init; }
    public required List<MtComponentScript> Scripts { get; init; }

    /// <summary>
    /// Creates a manialink-template instance from an TemplateFile instance.
    /// </summary>
    public static MtComponent FromTemplate(ManiaTemplateEngine engine, string templateContent,
        string? overwriteTag = null)
    {
        var foundComponents = new MtComponentMap();
        var namespaces = new List<string>();
        var foundProperties = new Dictionary<string, MtComponentProperty>();
        var maniaScripts = new Dictionary<int, MtComponentScript>();
        var componentTemplate = "";
        var hasSlot = false;

        var doc = new XmlDocument();
        doc.LoadXml(Helper.EscapePropertyTypes(templateContent));

        foreach (XmlNode node in doc.ChildNodes[0]!)
        {
            Debug.WriteLine($"Read node {node.OuterXml}");
            switch (node.Name)
            {
                case "property":
                    var property = ParseComponentProperty(node);
                    foundProperties.Add(property.Name, property);
                    break;

                case "import":
                    var component = ImportComponent(engine, node);
                    foundComponents.Add(component.Tag, component.Component);
                    break;

                case "using":
                    var ns = ParseComponentUsingStatement(node);
                    if (!namespaces.Contains(ns))
                    {
                        namespaces.Add(ns);
                    }

                    break;

                case "template":
                    componentTemplate = node.InnerXml;
                    hasSlot = NodeHasSlot(node);
                    break;

                case "script":
                    var script = MtComponentScript.FromNode(node);
                    var scriptContentHash = script.ContentHash();
                    if (script.Once && maniaScripts.ContainsKey(scriptContentHash))
                    {
                        break;
                    }

                    maniaScripts.Add(scriptContentHash, script);
                    break;
            }
        }

        return new MtComponent
        {
            Tag = overwriteTag ?? "",
            TemplateContent = componentTemplate,
            TemplateFileFile = new MtTemplateString { Content = templateContent },
            HasSlot = hasSlot,
            ImportedMtComponents = foundComponents,
            Properties = foundProperties,
            Namespaces = namespaces,
            Scripts = maniaScripts.Values.ToList()
        };
    }

    /// <summary>
    /// Parse an import-node and load the referenced component from the given directory.
    /// </summary>
    private static dynamic ImportComponent(ManiaTemplateEngine engine, XmlNode node)
    {
        string? tag = null, resource = null;

        foreach (XmlAttribute attribute in node.Attributes!)
        {
            switch (attribute.Name)
            {
                case "component":
                    resource = attribute.Value;
                    break;

                case "as":
                    tag = attribute.Value;
                    break;
            }
        }

        if (resource == null)
        {
            throw new Exception($"Missing required attribute 'component' for element '{node.OuterXml}'.");
        }

        tag ??= resource.Split('.')[^2];

        return new
        {
            Tag = tag,
            Component = resource
        };
    }

    /// <summary>
    /// Get the namespace of a XML-node, or throw an exception if none present.
    /// </summary>
    private static string ParseComponentUsingStatement(XmlNode node)
    {
        var nameSpace = GetNameSpaceAttributeValue(node.Attributes);

        if (nameSpace == null)
        {
            throw new Exception($"Missing attribute 'namespace' for element '{node.OuterXml}'.");
        }

        return nameSpace;
    }

    /// <summary>
    /// Returns the namespace-attribute value of a given XmlAttributeCollection.
    /// </summary>
    private static string? GetNameSpaceAttributeValue(XmlAttributeCollection? attributes)
    {
        return (from XmlAttribute attribute in attributes! where attribute.Name == "namespace" select attribute.Value)
            .FirstOrDefault();
    }


    private static MtComponentProperty ParseComponentProperty(XmlNode node)
    {
        string? name = null, type = null, defaultValue = null;

        foreach (XmlAttribute attribute in node.Attributes!)
        {
            switch (attribute.Name)
            {
                case "name":
                    name = attribute.Value;
                    break;

                case "type":
                    type = Helper.ReverseEscapeXmlAttributeString(attribute.Value);
                    break;

                case "default":
                    defaultValue = attribute.Value;
                    break;
            }
        }

        if (name == null)
        {
            throw new Exception($"Missing attribute 'name' for element '{node.OuterXml}'.");
        }

        if (type == null)
        {
            throw new Exception($"Missing attribute 'type' for element '{node.OuterXml}'.");
        }

        var property = new MtComponentProperty
        {
            Type = type,
            Name = name,
            Default = defaultValue
        };

        Debug.WriteLine($"Loaded property '{property.Name}' (default:{property.Default ?? "null"})");

        return property;
    }

    private static bool NodeHasSlot(XmlNode node)
    {
        if (node.Name == "slot")
        {
            return true;
        }

        return node.ChildNodes.Cast<XmlNode>()
            .Any(NodeHasSlot);
    }
}