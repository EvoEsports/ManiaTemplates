namespace ManiaTemplates.Components;

/// <summary>
/// Generates the documentation of components as markdown.
/// </summary>
public class MtComponentMarkdownGenerator
{
    /// <summary>
    /// The engine the components are loaded from.
    /// </summary>
    public required ManiaTemplateEngine Engine { get; init; }

    /// <summary>
    /// The components to document.
    /// </summary>
    public required MtComponentMap Components { get; init; }

    /// <summary>
    /// Generates a markdown document listing the given components with their attributes.
    /// </summary>
    public string Generate()
    {
        var output = new List<string>
        {
            "# Component list",
            "",
            "This list contains available components and their attributes.",
        };

        foreach (var (componentTag, componentImport) in Components)
        {
            var component = Engine.GetComponent(componentImport.TemplateKey);

            output.Add("");
            output.Add($"## {componentTag}");
            output.Add("");
            output.Add(component.HasSlot
                ? $"`<{componentTag}> ... </{componentTag}>`"
                : $"`<{componentTag} />`");

            if (component.Properties.Count == 0)
            {
                continue;
            }

            output.Add("");
            output.Add("| Attribute | Type | Default |");
            output.Add("|-----------|------|---------|");

            foreach (var property in component.Properties.Values)
            {
                output.Add($"| {property.Name} | {property.Type} | {property.Default} |");
            }
        }

        return string.Join('\n', output) + '\n';
    }
}
