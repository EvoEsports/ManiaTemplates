namespace ManiaTemplates.ControlElements;

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
        
        foreach (var (name, type) in this)
        {
            clone[name] = type;
        }
        
        foreach (var (name, type) in otherContext)
        {
            clone[name] = type; //TODO: prevent duplicate?
        }

        return clone;
    }

    public override string ToString()
    {
        return _name ?? "";
    }
}