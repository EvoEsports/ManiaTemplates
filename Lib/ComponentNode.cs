namespace ManiaTemplates.Lib;

public class ComponentNode
{
    public string Tag { get; }
    public Component Component { get; }
    public ComponentAttributes Attributes { get; }
    public string TemplateContent { get; }
    public string RenderId { get; }
    public string DataId { get; }
    public bool HasSlot { get; }
    public bool UsesComponents { get; }

    public ComponentNode(string tag, Component component, ComponentAttributes attributes, string templateContent, bool usesComponents)
    {
        Tag = tag;
        Component = component;
        Attributes = attributes;
        TemplateContent = templateContent;
        HasSlot = component.HasSlot;
        UsesComponents = usesComponents;
        RenderId = Helper.Hash(Tag+Component.TemplateFileFile.Checksum);
        DataId = Helper.RandomString(24);
    }
}