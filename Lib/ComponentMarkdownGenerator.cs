namespace ManiaTemplates.Lib;

public class ComponentMarkdownGenerator
{
    private readonly ComponentList _components;

    public ComponentMarkdownGenerator(ComponentList components)
    {
        _components = components;
    }

    public string Generate()
    {
        var output = new List<string>
        {
            "# Component list",
            "This list contains available components and their attributes.\n"
        };

        foreach (var (componentTag, component) in _components)
        {
            if (component.HasSlot)
            {
                output.Add($"``<{componentTag}> ... </{componentTag}>``\n");
            }
            else
            {
                output.Add($"``<{componentTag} />``\n");
            }
            
            output.Add("| Attribute  | Type   | Default |");
            output.Add("|------------|--------|---------|");

            foreach (var property in component.Properties.Values)
            {
                output.Add($"| {property.Name} | {property.Type} | {property.Default} |");
            }
        }

        return string.Join('\n', output);
    }
}