using System.Collections.Generic;

namespace ManiaTemplates.Components;

public class MtComponentMap : Dictionary<string, MtComponentImport>
{
    /// <summary>
    /// Add all of the given components to the existing ones. If a name already exists, it is replaced by the given component.
    /// </summary>
    public MtComponentMap Overload(Dictionary<string, MtComponentImport> components)
    {
        var subSet = new MtComponentMap();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet[name] = component;
        return subSet;
    }
}
