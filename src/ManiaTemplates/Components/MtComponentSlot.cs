namespace ManiaTemplates.Components;

public class MtComponentSlot
{
    public required string Content;
    public required MtComponent Component;
    public required MtComponent ParentComponent;
    public required MtComponentContext Context;

    public string RenderMethodName(string renderContextId)
    {
        return "RenderSlot_" + renderContextId;
    }
}