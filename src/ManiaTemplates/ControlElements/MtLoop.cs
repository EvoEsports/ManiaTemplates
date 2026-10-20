namespace ManiaTemplates.ControlElements;

public abstract class MtLoop
{
    public required string Condition { get; init; }
    public required List<MtLoopVariable> Variables { get; init; }
    public required MtDataContext Context { get; init; }

    /// <summary>
    /// The nesting depth of the loop, starting at 1 for the outermost loop.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// The C# keyword starting the loop (for, foreach).
    /// </summary>
    public abstract string Keyword { get; }

    /// <summary>
    /// The name of the variable containing the zero-based iteration index, available inside the loop.
    /// </summary>
    public string IndexVariableName => GetIndexVariableName(Depth);

    /// <summary>
    /// The name of the variable counting the iterations, used to set the index variable.
    /// </summary>
    public string CounterVariableName => $"__loopCounter{Depth}";

    private static string GetIndexVariableName(int depth)
    {
        return depth > 1 ? $"__index{depth}" : "__index";
    }

    /// <summary>
    /// Creates the data context of the loop body, containing the loop variables, the index variable and all
    /// variables of the surrounding context.
    /// </summary>
    protected static MtDataContext CreateContext(string name, int depth, List<MtLoopVariable> variables,
        MtDataContext parentContext)
    {
        var loopContext = new MtDataContext(name)
        {
            { GetIndexVariableName(depth), "int" },
        };

        foreach (var variable in variables)
        {
            loopContext[variable.Name] = variable.Type;
        }

        return loopContext.NewContext(parentContext);
    }

    /// <summary>
    /// Returns the name of a variable that is declared twice by the loop or already declared by a surrounding loop,
    /// else null.
    /// </summary>
    protected static string? FindRedeclaredVariableName(IEnumerable<MtLoopVariable> variables,
        MtDataContext parentContext)
    {
        var declaredNames = new HashSet<string>(parentContext.Keys);
        return variables.Select(variable => variable.Name).FirstOrDefault(name => !declaredNames.Add(name));
    }

    /// <summary>
    /// Whether any of the variables uses a name reserved for internal variables (starting with __).
    /// </summary>
    protected static bool UsesReservedVariableName(IEnumerable<MtLoopVariable> variables)
    {
        return variables.Any(variable => variable.Name.StartsWith("__"));
    }
}
