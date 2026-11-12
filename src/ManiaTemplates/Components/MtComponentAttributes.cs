using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Components;

/// <summary>
/// The attributes of a node, mapped by their names.
/// </summary>
public class MtComponentAttributes : Dictionary<string, MtComponentAttribute>
{
    /// <summary>
    /// Checks the attribute list for a condition (if, else-if, else) and if found removes & returns it, else null.
    /// </summary>
    public MtCondition? PullCondition()
    {
        var conditions = new List<MtCondition>();

        if (ContainsKey("if"))
        {
            conditions.Add(new MtCondition { Type = MtConditionType.If, Expression = Pull("if") });
        }

        if (ContainsKey("else-if"))
        {
            conditions.Add(new MtCondition { Type = MtConditionType.ElseIf, Expression = Pull("else-if") });
        }

        if (ContainsKey("else"))
        {
            if (Pull("else").Trim().Length > 0)
            {
                throw new InvalidConditionException(
                    "The else attribute must not have a value, use else-if to add a condition.");
            }

            conditions.Add(new MtCondition { Type = MtConditionType.Else });
        }

        if (conditions.Count > 1)
        {
            throw new InvalidConditionException(
                $"A node must only have one of the attributes {string.Join(", ", conditions.Select(c => c.AttributeName))}.");
        }

        var condition = conditions.SingleOrDefault();
        if (condition is { Type: not MtConditionType.Else } && condition.Expression.Trim().Length == 0)
        {
            throw new EmptyNodeAttributeException($"The {condition.AttributeName} attribute must not be empty.");
        }

        return condition;
    }

    /// <summary>
    /// Checks the attribute list for loop-condition and if found removes & returns it, else null.
    /// </summary>
    /// <param name="context">The variables available at the node.</param>
    /// <param name="nodeId">The id of the node, used for the name of the loop context.</param>
    /// <param name="depth">The nesting depth of the loop, starting at 1 for the outermost loop.</param>
    public MtLoop? PullLoop(MtDataContext context, int nodeId, int depth)
    {
        var hasForeach = ContainsKey("foreach");
        var hasFor = ContainsKey("for");

        if (hasForeach && hasFor)
        {
            throw new ParsingForLoopFailedException("A node must not have both a for and a foreach attribute.");
        }

        if (hasForeach)
        {
            return MtForeach.FromString(Pull("foreach"), context, nodeId, depth);
        }

        return hasFor ? MtFor.FromString(Pull("for"), context, nodeId, depth) : null;
    }

    /// <summary>
    /// Checks the attribute list for name and if found removes & returns it, else "default".
    /// </summary>
    public string PullName()
    {
        return ContainsKey("name") ? Pull("name") : "default";
    }

    /// <summary>
    /// Removes the attribute with the given name and returns its value.
    /// </summary>
    /// <param name="name">The name of the attribute.</param>
    public string Pull(string name)
    {
        var value = this[name];
        Remove(name);
        return value.Value;
    }
}