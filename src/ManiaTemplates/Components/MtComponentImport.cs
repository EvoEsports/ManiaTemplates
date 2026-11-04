namespace ManiaTemplates.Components;

/// <summary>
/// A component imported into a template, used with the tag and rendered from the template with the key.
/// </summary>
public class MtComponentImport
{
    /// <summary>
    /// The key of the imported template.
    /// </summary>
    public required string TemplateKey;

    /// <summary>
    /// The tag the component is used with.
    /// </summary>
    public required string Tag;
}