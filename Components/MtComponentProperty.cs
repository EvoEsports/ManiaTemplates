namespace ManiaTemplates.Components;

public class MtComponentProperty
{
    public string Type { get; }
    public string Name { get; }
    public string? Default { get; }

    public MtComponentProperty(string type, string name, string? defaultValue = null)
    {
        Type = type;
        Name = name;
        Default = defaultValue;
    }
}