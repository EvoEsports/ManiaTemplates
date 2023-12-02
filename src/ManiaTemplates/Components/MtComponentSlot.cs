using ManiaTemplates.ControlElements;

namespace ManiaTemplates.Components;

public class MtComponentSlot
{
    public required int Scope { get; init; }
    public required string RenderMethod { get; init; }
    public required MtDataContext Context { get; init; }
    public string Name { get; init; } = "default";
}