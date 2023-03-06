namespace ManiaTemplates.Components;

public class MtComponentMarkdownGenerator
{
    public required MtComponentMap Components { get; init; }
    
    public string Generate()
    {
        var output = new List<string>
        {
            "# Component list",
            "This list contains available components and their attributes.\n"
        };

        // foreach (var (componentTag, component) in Components)
        // {
        //     if (component.HasSlot)
        //     {
        //         output.Add($"``<{componentTag}> ... </{componentTag}>``");
        //     }
        //     else
        //     {
        //         output.Add($"``<{componentTag} />``");
        //     }
        //     
        //     output.Add("\n\n");
        //     output.Add("| Attribute  | Type   | Default |");
        //     output.Add("|------------|--------|---------|");
        //
        //     foreach (var property in component.Properties.Values)
        //     {
        //         output.Add($"| {property.Name} | {property.Type} | {property.Default} |");
        //     }
        // }

        return string.Join('\n', output);
    }
}