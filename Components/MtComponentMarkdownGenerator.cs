namespace ManiaTemplates.Components;

public class MtComponentMarkdownGenerator
{
    public required MtComponentMap Components { get; init; }
    
    public string Generate(ManiaTemplateEngine engine)
    {
        var output = new List<string>
        {
            "# Component list",
            "This list contains available components and their attributes.\n"
        };

        foreach (var (componentTag, componentPath) in Components)
        {
            var component = engine.GetComponent(componentPath);
            if (component.HasSlot)
            {
                output.Add($"``<{componentTag}> ... </{componentTag}>``");
            }
            else
            {
                output.Add($"``<{componentTag} />``");
            }
            
            output.Add("\n\n");
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