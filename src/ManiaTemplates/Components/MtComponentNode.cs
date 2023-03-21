namespace ManiaTemplates.Components;

public class MtComponentNode
{
    public string Tag { get; }
    public MtComponent MtComponent { get; }
    public MtDataContext Context { get; }
    public MtComponentAttributes Attributes { get; }
    public string TemplateContent { get; }
    public bool HasSlot { get; }

    public MtComponentNode(string tag, MtComponent mtComponent, MtComponentAttributes attributes,
        string templateContent, MtDataContext context)
    {
        Tag = tag;
        MtComponent = mtComponent;
        Attributes = attributes;
        TemplateContent = templateContent;
        HasSlot = mtComponent.HasSlot;
        Context = context;
    }
}