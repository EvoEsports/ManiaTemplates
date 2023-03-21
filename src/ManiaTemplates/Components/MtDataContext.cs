namespace ManiaTemplates.Components;

public class MtDataContext : Dictionary<string, string>
{
    public MtDataContext? ParentContext { get; private init; }

    public MtDataContext NewContext(MtDataContext otherContext)
    {
        var clone = new MtDataContext { ParentContext = this };

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