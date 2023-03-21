namespace ManiaTemplates.Components;

public class MtComponentAttributes : Dictionary<string, string>
{
    public string Pull(string name)
    {
        var value = this[name];
        Remove(name);
        return value;
    }
}