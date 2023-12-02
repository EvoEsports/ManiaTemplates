using System.Diagnostics;
using System.Xml;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Components;

public class MtComponent
{
    public required string TemplateContent { get; init; }
    public required bool HasSlot { get; init; }
    public string? DisplayLayer { get; init; }
    public required MtComponentMap ImportedComponents { get; init; }
    public required Dictionary<string, MtComponentProperty> Properties { get; init; }
    public required List<string> Namespaces { get; init; }
    public required List<MtComponentScript> Scripts { get; init; }
    public List<string> Slots { get; set; } = new();

    /// <summary>
    /// Creates a MtComponent instance from template contents.
    /// </summary>
    public static MtComponent FromTemplate(ManiaTemplateEngine engine, string templateContent)
    {
        var foundComponents = new MtComponentMap();
        var namespaces = new List<string>();
        var foundProperties = new Dictionary<string, MtComponentProperty>();
        var maniaScripts = new List<MtComponentScript>();
        var slots = new List<string>();
        var componentTemplate = "";
        var hasSlot = false;
        string? layer = null;
        var rootNode = FindComponentNode(templateContent);

        foreach (XmlNode node in rootNode.ChildNodes)
        {
            Debug.WriteLine($"Read node {node.OuterXml}");
            switch (node.Name)
            {
                case "property":
                    var property = ParseComponentProperty(node);
                    foundProperties.Add(property.Name, property);
                    break;

                case "import":
                    var componentImport = ParseImportNode(engine, node);
                    foundComponents.Add(componentImport.Tag, componentImport);
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
                    layer = ParseDisplayLayer(node);
                    hasSlot = NodeHasSlot(node);
                    slots = GetSlotNamesInTemplate(node);
                    break;

                case "script":
                    var script = MtComponentScript.FromNode(engine, node);
                    maniaScripts.Add(script);
                    break;
            }
        }

        return new MtComponent
        {
            TemplateContent = componentTemplate,
            HasSlot = hasSlot,
            Slots = slots,
            ImportedComponents = foundComponents,
            Properties = foundProperties,
            Namespaces = namespaces,
            Scripts = maniaScripts,
            DisplayLayer = layer
        };
    }

    /// <summary>
    /// Looks for the component root node in a given ManiaTemplate string
    /// </summary>
    private static XmlNode FindComponentNode(string templateContent)
    {
        var doc = new XmlDocument();
        doc.LoadXml(Helper.EscapePropertyTypes(templateContent));

        foreach (XmlNode node in doc.ChildNodes)
        {
            if (node.Name.ToLower() == "component")
            {
                return node;
            }
        }

        throw new MissingComponentRootException($"Could not find <component> node in: {templateContent}");
    }

    /// <summary>
    /// Parse an import-node and load the referenced component from the given directory.
    /// </summary>
    private static MtComponentImport ParseImportNode(ManiaTemplateEngine engine, XmlNode node)
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
            throw new MissingAttributeException(
                $"Missing required attribute 'component' for element '{node.OuterXml}'.");
        }

        tag ??= resource.Split('.')[^2];

        return new MtComponentImport
        {
            TemplateKey = resource,
            Tag = tag,
        };
    }

    /// <summary>
    /// Gets the layer value of a template node in a component file.
    /// </summary>
    private static string? ParseDisplayLayer(XmlNode node)
    {
        return (from XmlAttribute attribute in node.Attributes! where attribute.Name == "layer" select attribute.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// Get the namespace of a XML-node, or throw an exception if none present.
    /// </summary>
    private static string ParseComponentUsingStatement(XmlNode node)
    {
        var nameSpace = GetNameSpaceAttributeValue(node.Attributes);

        if (nameSpace == null)
        {
            throw new MissingAttributeException($"Missing attribute 'namespace' for element '{node.OuterXml}'.");
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
            throw new MissingAttributeException($"Missing attribute 'name' for element '{node.OuterXml}'.");
        }

        if (type == null)
        {
            throw new MissingAttributeException($"Missing attribute 'type' for element '{node.OuterXml}'.");
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

    /// <summary>
    /// Gets all slot names recursively.
    /// </summary>
    private static List<string> GetSlotNamesInTemplate(XmlNode node)
    {
        var slotNames = new List<string>();

        if (node.Name == "slot")
        {
            var slotName = node.Attributes?["name"]?.Value.ToLower() ?? "default";

            if (slotNames.Contains(slotName))
            {
                throw new DuplicateSlotException($"""A slot with the name "{slotName}" already exists.""");
            }

            slotNames.Add(slotName);
        }
        else if (node.HasChildNodes)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                slotNames.AddRange(GetSlotNamesInTemplate(childNode));
            }
        }

        return slotNames;
    }

    public string Id()
    {
        return "MtContext" + GetHashCode().ToString().Replace("-", "N");
    }
}