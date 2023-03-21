namespace ManiaTemplates.Components;

public class MtComponentContext : Dictionary<string, string>
{
    public MtComponentContext? ParentContext { get; private init; }

    public MtComponentContext NewContext(MtComponentContext otherContext)
    {
        var clone = new MtComponentContext { ParentContext = this };

        foreach (var (name, type) in otherContext)
        {
            clone[name] = type;
        }

        return clone;
    }

    public override string ToString()
    {
        return "MtContext" + GetHashCode().ToString().Replace("-", "N");
    }
}