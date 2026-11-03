namespace ManiaTemplates.ControlElements;

public class MtDataContext : Dictionary<string, string>
{
    private readonly string? _name;

    public MtDataContext(string? name = null)
    {
        _name = name;
    }

    private MtDataContext(string? name, IDictionary<string, string> variables) : base(variables)
    {
        _name = name;
    }

    /// <summary>
    /// Creates a context containing the variables of this and the other context, the other one takes precedence.
    /// </summary>
    public MtDataContext NewContext(MtDataContext otherContext)
    {
        var merged = new MtDataContext($"{_name}_{otherContext._name}", this);
        foreach (var (name, type) in otherContext)
        {
            merged[name] = type;
        }

        return merged;
    }

    /// <summary>
    /// Creates a context with the same name and variables, which can be extended without changing this one.
    /// </summary>
    public MtDataContext Copy() => new(_name, this);

    public override string ToString()
    {
        return _name ?? "";
    }
}
