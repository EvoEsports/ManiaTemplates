using System.Diagnostics;
using System.Xml;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Components;

public class MtComponent
{
    public required string Tag { get; init; }
    public required string TemplateContent { get; init; }
    public required TemplateFile TemplateFileFile { get; init; }
    public required bool HasSlot { get; init; }
    public required MtComponentList ImportedMtComponents { get; init; }
    public required Dictionary<string, MtComponentProperty> Properties { get; init; }
    public required List<string> Namespaces { get; init; }

    /// <summary>
    /// Creates a manialink-template instance from given file content.
    /// </summary>
    public static MtComponent FromFile(string filename) => FromTemplate(new TemplateFile(filename));

    /// <summary>
    /// Creates a manialink-template instance from an TemplateFile instance.
    /// </summary>
    internal static MtComponent FromTemplate(TemplateFile templateFile, string? overwriteTag = null)
    {
        Debug.WriteLine(
            $"Loading component ({templateFile.Name}|{templateFile.LastModification}) from {templateFile.TemplatePath}");

        var foundComponents = new MtComponentList();
        var namespaces = new List<string>();
        var foundProperties = new Dictionary<string, MtComponentProperty>();
        var componentTemplate = "";
        var hasSlot = false;

        var doc = new XmlDocument();
        doc.LoadXml(Helper.EscapePropertyTypes(templateFile.Content()));

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
                    var component = LoadComponent(node, templateFile.Directory() ?? "");
                    foundComponents.Add(component.Tag, component);
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
            }
        }

        return new MtComponent
        {
            Tag = overwriteTag ?? templateFile.Name,
            TemplateContent = componentTemplate,
            TemplateFileFile = templateFile,
            HasSlot = hasSlot,
            ImportedMtComponents = foundComponents,
            Properties = foundProperties,
            Namespaces = namespaces
        };
    }

    /// <summary>
    /// Parse an import-node and load the referenced component from the given directory.
    /// </summary>
    private static MtComponent LoadComponent(XmlNode node, string currentDirectory)
    {
        string? src = null, importAs = null;

        foreach (XmlAttribute attribute in node.Attributes!)
        {
            switch (attribute.Name)
            {
                case "src":
                    src = attribute.Value;
                    break;

                case "as":
                    importAs = attribute.Value;
                    break;
            }
        }

        if (src == null)
        {
            throw new Exception($"Missing attribute 'src' for element '{node.OuterXml}'.");
        }

        var subPath = Path.GetFullPath(Path.Combine(currentDirectory ?? "", src));
        Debug.WriteLine("new path: " + subPath);
        var component = FromTemplate(new TemplateFile(subPath), importAs);

        Debug.WriteLine($"Loaded sub-component '{component.Tag}'");

        return component;
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