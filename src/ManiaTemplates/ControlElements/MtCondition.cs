namespace ManiaTemplates.ControlElements;

/// <summary>
/// The attribute a condition is defined with.
/// </summary>
public enum MtConditionType
{
    /// <summary>
    /// Renders the node if the expression is true.
    /// </summary>
    If,

    /// <summary>
    /// Renders the node if no previous condition of the chain was met and the expression is true.
    /// </summary>
    ElseIf,

    /// <summary>
    /// Renders the node if no previous condition of the chain was met.
    /// </summary>
    Else
}

/// <summary>
/// A condition placed on a node with an if, else-if or else attribute.
/// </summary>
public class MtCondition
{
    /// <summary>
    /// Whether the condition starts or continues a condition chain.
    /// </summary>
    public required MtConditionType Type { get; init; }

    /// <summary>
    /// The condition expression, empty for else.
    /// </summary>
    public string Expression { get; init; } = "";

    /// <summary>
    /// The name of the attribute the condition was defined with.
    /// </summary>
    public string AttributeName => Type switch
    {
        MtConditionType.If => "if",
        MtConditionType.ElseIf => "else-if",
        _ => "else"
    };
}
