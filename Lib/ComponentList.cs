namespace ManiaTemplates.Lib;

public class ComponentList : Dictionary<string, Component>
{
    public ComponentList Overload(Dictionary<string, Component> components)
    {
        var subSet = new ComponentList();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet.Add(name, component);
        return subSet;
    }
}