namespace ManiaTemplates.Components;

/// <summary>
/// An attribute of a node, which is either passed to a component property or rendered on the node.
/// </summary>
public class MtComponentAttribute
{
    /// <summary>
    /// The value of the attribute.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Whether the attribute is passed through a component to its root node, where it is only rendered if set.
    /// </summary>
    public bool IsFallthroughAttribute { get; init; }

    /// <summary>
    /// The variable containing the value of a fallthrough attribute.
    /// </summary>
    public string? Alias { get; init; }
}