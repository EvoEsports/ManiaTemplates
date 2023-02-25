using ManiaTemplates.TemplateSources;

namespace ManiaTemplates.Components;

public class MtComponentList : Dictionary<string, MtComponent>
{
    /// <summary>
    /// Add all of the given components to the existing ones. If a name already exists, it is replaced by the given component.
    /// </summary>
    public MtComponentList Overload(Dictionary<string, MtComponent> components)
    {
        var subSet = new MtComponentList();
        foreach (var (name, component) in this) subSet.Add(name, component);
        foreach (var (name, component) in components) subSet.Add(name, component);
        return subSet;
    }

    public void AddResource(string resource)
    {
        var template = new MtTemplateResource { ResourcePath = resource };
        var component = MtComponent.FromTemplate(template);
        Add(template.GetXmlTag(), component);
    }
}