namespace ManiaTemplates.Components;

public class MtDataContext : Dictionary<string, string>
{
    public MtDataContext? ParentContext { get; }
    
    private readonly string? _name;
    private readonly List<MtDataContext>? _previousContexts;

    public MtDataContext(string? name = null, List<MtDataContext>? previousContexts = null)
    {
        _name = name;
        _previousContexts = previousContexts;

        if (previousContexts != null && previousContexts.Count > 0)
        {
            ParentContext = previousContexts.Last();
        }
    }

    public MtDataContext NewContext(MtDataContext otherContext)
    {
        var previous = _previousContexts ?? new List<MtDataContext>();
        previous.Add(this);

        var clone = new MtDataContext($"{_name}_{otherContext._name}", previous);
        foreach (var (name, type) in otherContext)
        {
            clone[name] = type;
        }

        return clone;
    }

    public override string ToString()
    {
        return $"C{_name}";
        // return $"MtC_{Name}_" + GetHashCode().ToString().Replace("-", "N");
    }
}