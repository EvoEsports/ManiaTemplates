namespace ManiaTemplates.Lib;

public class ComponentProperty
{
    public string Type { get; }
    public string Name { get; }
    public string? Default { get; }

    public ComponentProperty(string type, string name, string? defaultValue = null)
    {
        Type = type;
        Name = name;
        Default = defaultValue;
    }
}