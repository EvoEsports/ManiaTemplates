namespace ManiaTemplates.ControlElements;

/// <summary>
/// The variables available at a position in a template, mapping their names to their C# types.
/// </summary>
public class MtDataContext : Dictionary<string, string>
{
    private readonly string? _name;

    /// <summary>
    /// Creates an empty context, the name is part of the names of generated render methods.
    /// </summary>
    /// <param name="name">The name of the context.</param>
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
    /// <param name="otherContext">The context whose variables are added.</param>
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
