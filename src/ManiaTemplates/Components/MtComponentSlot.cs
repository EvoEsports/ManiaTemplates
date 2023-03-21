namespace ManiaTemplates.Components;

public class MtComponentSlot
{
    public required string Content;
    public required MtDataContext Context;

    public string RenderMethodName(string renderContextId)
    {
        return "RenderSlot_" + renderContextId;
    }
}