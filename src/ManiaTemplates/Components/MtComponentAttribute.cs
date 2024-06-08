namespace ManiaTemplates.Components;

public class MtComponentAttribute
{
    public required string Value { get; init; }
    public bool IsFallthroughAttribute { get; init; }
    public string? Alias { get; init; }
}