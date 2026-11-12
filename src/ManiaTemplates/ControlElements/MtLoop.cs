namespace ManiaTemplates.ControlElements;

/// <summary>
/// A loop placed on a node, which renders the node once per iteration.
/// </summary>
public abstract class MtLoop
{
    /// <summary>
    /// The value of the loop attribute, placed in the parentheses of the generated loop.
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// The variables declared by the loop.
    /// </summary>
    public required List<MtLoopVariable> Variables { get; init; }

    /// <summary>
    /// The variables available in the loop body.
    /// </summary>
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
    /// <param name="name">The name of the loop context.</param>
    /// <param name="depth">The nesting depth of the loop.</param>
    /// <param name="variables">The variables declared by the loop.</param>
    /// <param name="parentContext">The variables available at the node.</param>
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
    /// <param name="variables">The variables declared by the loop.</param>
    /// <param name="parentContext">The variables available at the node.</param>
    protected static string? FindRedeclaredVariableName(IEnumerable<MtLoopVariable> variables,
        MtDataContext parentContext)
    {
        var declaredNames = new HashSet<string>(parentContext.Keys);
        return variables.Select(variable => variable.Name).FirstOrDefault(name => !declaredNames.Add(name));
    }

    /// <summary>
    /// Whether any of the variables uses a name reserved for internal variables (starting with __).
    /// </summary>
    /// <param name="variables">The variables declared by the loop.</param>
    protected static bool UsesReservedVariableName(IEnumerable<MtLoopVariable> variables)
    {
        return variables.Any(variable => variable.Name.StartsWith("__"));
    }
}
