namespace ManiaTemplates.Components;

public class MtComponentSlot
{
    public required int Scope { get; init; }
    public required string RenderMethodT4 { get; init; }
    public string Name { get; init; } = "default";
}