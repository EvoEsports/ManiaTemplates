namespace ManiaTemplates.Components;

public class MtComponentNode
{
    public string Tag { get; }
    public MtComponent MtComponent { get; }
    public MtComponent ParentComponent { get; }
    public MtComponentContext Context { get; }
    public MtComponentAttributes Attributes { get; }
    public string TemplateContent { get; }
    public string RenderId { get; }
    public bool HasSlot { get; }
    public bool UsesComponents { get; }

    public MtComponentNode(string tag, MtComponent mtComponent, MtComponentAttributes attributes,
        string templateContent, bool usesComponents, MtComponent parentComponent, MtComponentContext context)
    {
        Tag = tag;
        MtComponent = mtComponent;
        Attributes = attributes;
        TemplateContent = templateContent;
        HasSlot = mtComponent.HasSlot;
        UsesComponents = usesComponents;
        RenderId = MtComponent.GetHashCode().ToString().Replace("-", "N");
        ParentComponent = parentComponent;
        Context = context;
    }
}