namespace ManiaTemplates.Components;

public class MtDataContext : Dictionary<string, string>
{
    public MtDataContext? ParentContext { get; }
    private readonly string? _name;

    public MtDataContext(string? name = null, MtDataContext? previousContext = null)
    {
        _name = name;
        ParentContext = previousContext;
    }

    public MtDataContext NewContext(MtDataContext otherContext)
    {
        var clone = new MtDataContext($"{_name}_{otherContext._name}", this);
        foreach (var (name, type) in otherContext)
        {
            clone[name] = type;
        }

        return clone;
    }

    public override string ToString()
    {
        return $"C{_name}";
        // return $"MtContext_{_name}_" + GetHashCode().ToString().Replace("-", "N");
    }
}