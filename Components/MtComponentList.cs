namespace ManiaTemplates.Components;

public class MtComponentList : Dictionary<string, MtComponent>
{
    public MtComponentList Overload(Dictionary<string, MtComponent> components)
    {
        var subSet = new MtComponentList();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet.Add(name, component);
        return subSet;
    }
}