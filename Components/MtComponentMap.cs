namespace ManiaTemplates.Components;

public class MtComponentMap : Dictionary<string, MtComponent>
{
    /// <summary>
    /// Add all of the given components to the existing ones. If a name already exists, it is replaced by the given component.
    /// </summary>
    public MtComponentMap Overload(Dictionary<string, MtComponent> components)
    {
        var subSet = new MtComponentMap();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet[name] = component;
        return subSet;
    }
}