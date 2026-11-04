using System.Xml;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Components;

/// <summary>
/// A parsed template with its imports, properties, scripts and slots.
/// </summary>
public class MtComponent
{
    /// <summary>
    /// The contents of the template node, which are rendered by the component.
    /// </summary>
    public required string TemplateContent { get; init; }

    /// <summary>
    /// Whether the template contains a slot.
    /// </summary>
    public required bool HasSlot { get; init; }

    /// <summary>
    /// The layer the manialink is displayed on, set with the layer attribute of the template node.
    /// </summary>
    public string? DisplayLayer { get; init; }

    /// <summary>
    /// The components imported by the template.
    /// </summary>
    public required MtComponentMap ImportedComponents { get; init; }

    /// <summary>
    /// The properties of the component, mapped by their names.
    /// </summary>
    public required Dictionary<string, MtComponentProperty> Properties { get; init; }

    /// <summary>
    /// The C# namespaces the template uses, imported with using nodes.
    /// </summary>
    public required List<string> Namespaces { get; init; }

    /// <summary>
    /// The ManiaScripts of the component.
    /// </summary>
    public required List<MtComponentScript> Scripts { get; init; }

    /// <summary>
    /// The names of the slots of the component, the unnamed slot is called default.
    /// </summary>
    public HashSet<string> Slots { get; init; } = new();

    /// <summary>
    /// Creates a MtComponent instance from template contents.
    /// </summary>
    /// <param name="engine">The engine providing the ManiaScripts referenced by the template.</param>
    /// <param name="templateContent">The contents of the template.</param>
    public static MtComponent FromTemplate(ManiaTemplateEngine engine, string templateContent)
    {
        var foundComponents = new MtComponentMap();
        var namespaces = new List<string>();
        var foundProperties = new Dictionary<string, MtComponentProperty>();
        var maniaScripts = new List<MtComponentScript>();
        var slots = new HashSet<string>();
        var componentTemplate = "";
        string? layer = null;
        var rootNode = FindComponentNode(templateContent);

        foreach (XmlNode node in rootNode.ChildNodes)
        {
            switch (node.Name)
            {
                case "property":
                    var property = ParseComponentProperty(node);
                    foundProperties.Add(property.Name, property);
                    break;

                case "import":
                    var componentImport = ParseImportNode(node);
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
                    layer = node.Attributes?["layer"]?.Value;
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
            HasSlot = slots.Count > 0,
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
        doc.LoadXml(XmlAttributeEscaper.EscapeXmlSpecialCharsInAttributes(templateContent));

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
    /// Parses an import-node, the tag defaults to the name of the template key like "Folder.Component.mt".
    /// </summary>
    private static MtComponentImport ParseImportNode(XmlNode node)
    {
        var resource = node.Attributes?["component"]?.Value ?? throw new MissingAttributeException(
            $"Missing required attribute 'component' for element '{node.OuterXml}'.");

        return new MtComponentImport
        {
            TemplateKey = resource,
            Tag = node.Attributes["as"]?.Value ?? resource.Split('.')[^2],
        };
    }

    /// <summary>
    /// Get the namespace of a using-node, or throw an exception if none present.
    /// </summary>
    private static string ParseComponentUsingStatement(XmlNode node)
    {
        return node.Attributes?["namespace"]?.Value ??
               throw new MissingAttributeException($"Missing attribute 'namespace' for element '{node.OuterXml}'.");
    }

    /// <summary>
    /// Parses a property-node, which needs a name and type.
    /// </summary>
    private static MtComponentProperty ParseComponentProperty(XmlNode node)
    {
        var name = node.Attributes?["name"]?.Value ??
                   throw new MissingAttributeException($"Missing attribute 'name' for element '{node.OuterXml}'.");
        var type = node.Attributes["type"]?.Value ??
                   throw new MissingAttributeException($"Missing attribute 'type' for element '{node.OuterXml}'.");

        if (!StringUtils.IsVariableName(name))
        {
            throw new InvalidPropertyNameException(
                $"The property name '{name}' must be a valid C# variable name, not a C# keyword and not starting with __.");
        }

        return new MtComponentProperty
        {
            Type = type,
            Name = name,
            Default = node.Attributes["default"]?.Value
        };
    }

    /// <summary>
    /// Gets all slot names recursively.
    /// </summary>
    private static HashSet<string> GetSlotNamesInTemplate(XmlNode node)
    {
        var slotNames = new HashSet<string>();

        if (node.Name == "slot")
        {
            slotNames.Add(node.Attributes?["name"]?.Value.ToLower() ?? "default");
        }
        else if (node.HasChildNodes)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                slotNames.UnionWith(GetSlotNamesInTemplate(childNode));
            }
        }

        return slotNames;
    }

    /// <summary>
    /// Returns a name unique to this instance, used for generated code.
    /// </summary>
    public string Id()
    {
        return "MtContext" + GetHashCode().ToString().Replace("-", "N");
    }
}