namespace ManiaTemplates.Components;

/// <summary>
/// The components available in a template, mapping their tags to the imported templates.
/// </summary>
public class MtComponentMap : Dictionary<string, MtComponentImport>
{
    /// <summary>
    /// Returns a new map with the components of this one and the given ones. Given components replace existing ones with
    /// the same tag.
    /// </summary>
    /// <param name="components">The components to add.</param>
    public MtComponentMap Overload(Dictionary<string, MtComponentImport> components)
    {
        var subSet = new MtComponentMap();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet[name] = component;
        return subSet;
    }
}
