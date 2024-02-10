namespace ManiaTemplates.Components;

public class MtComponentProperty
{
    public required string Type { get; init; }
    public required string Name { get; init; }
    public string? Default { get; init; }

    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    public bool IsStringType()
    {
        return Type.ToLower().Contains("string"); //TODO: find better way to determine string
    }
}