﻿using System.Diagnostics;
using System.Xml;

namespace ManiaTemplates.Lib;

public class Component
{
    public string Tag { get; }
    public string TemplateContent { get; }
    public TemplateFile TemplateFileFile { get; }
    public bool HasSlot { get; }
    public ComponentList ImportedComponents { get; }
    public Dictionary<string, ComponentProperty> Properties { get; }

    public List<string> Namespaces { get; }

    private Component(string tag, ComponentList importedComponents,
        Dictionary<string, ComponentProperty> importedProperties,
        string templateContent, bool hasSlot, TemplateFile templateFileFile, List<string> namespaces)
    {
        Tag = tag;
        TemplateFileFile = templateFileFile;
        TemplateContent = templateContent;
        ImportedComponents = importedComponents;
        Properties = importedProperties;
        HasSlot = hasSlot;
        Namespaces = namespaces;
    }

    public static Component FromFile(string filename)
    {
        return FromTemplate(new TemplateFile(filename));
    }

    internal static Component FromTemplate(TemplateFile templateFile, string? overwriteTag = null)
    {
        Debug.WriteLine(
            $"Loading component ({templateFile.Name}|{templateFile.LastModification}) from {templateFile.TemplatePath}");

        var foundComponents = new ComponentList();
        var namespaces = new List<string>();
        var foundProperties = new Dictionary<string, ComponentProperty>();
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
        
        return new Component(overwriteTag ?? templateFile.Name, foundComponents, foundProperties, componentTemplate,
            hasSlot, templateFile, namespaces);
    }

    private static Component LoadComponent(XmlNode node, string currentDirectory)
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

    private static string ParseComponentUsingStatement(XmlNode node)
    {
        string? ns = null;

        foreach (XmlAttribute attribute in node.Attributes!)
        {
            if (attribute.Name == "namespace")
            {
                ns = attribute.Value;
                break;
            }
        }

        if (ns == null)
        {
            throw new Exception($"Missing attribute 'namespace' for element '{node.OuterXml}'.");
        }

        return ns;
    }


    private static ComponentProperty ParseComponentProperty(XmlNode node)
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

        var property = new ComponentProperty(type, name, defaultValue);

        Debug.WriteLine($"Loaded property '{property.Name}' (default:{property.Default ?? "null"})");

        return property;
    }

    private static bool NodeHasSlot(XmlNode node)
    {
        if (node.Name == "slot")
        {
            return true;
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            if (NodeHasSlot(child))
            {
                return true;
            }
        }

        return false;
    }
}