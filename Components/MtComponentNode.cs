using ManiaTemplates.Lib;

namespace ManiaTemplates.Components;

public class MtComponentNode
{
    public string Tag { get; }
    public MtComponent MtComponent { get; }
    public MtComponentAttributes Attributes { get; }
    public string TemplateContent { get; }
    public string RenderId { get; }
    public string DataId { get; }
    public bool HasSlot { get; }
    public bool UsesComponents { get; }

    public MtComponentNode(string tag, MtComponent mtComponent, MtComponentAttributes attributes, string templateContent, bool usesComponents)
    {
        Tag = tag;
        MtComponent = mtComponent;
        Attributes = attributes;
        TemplateContent = templateContent;
        HasSlot = mtComponent.HasSlot;
        UsesComponents = usesComponents;
        RenderId = Helper.Hash(MtComponent.TemplateFileFile.GetHashCode() + "");
        DataId = Helper.RandomString(24);
    }
}