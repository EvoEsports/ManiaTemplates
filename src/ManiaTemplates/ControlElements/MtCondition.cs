namespace ManiaTemplates.ControlElements;

public enum MtConditionType
{
    If,
    ElseIf,
    Else
}

public class MtCondition
{
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

    /// <summary>
    /// Whether the condition is evaluated for each iteration when combined with a loop on the same node.
    /// Conditions continuing a chain (else-if, else) are evaluated once and wrap the loop instead.
    /// </summary>
    public bool IsEvaluatedPerLoopIteration => Type == MtConditionType.If;
}
