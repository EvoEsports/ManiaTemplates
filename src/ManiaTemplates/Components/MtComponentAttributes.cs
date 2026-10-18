using ManiaTemplates.ControlElements;

namespace ManiaTemplates.Components;

public class MtComponentAttributes : Dictionary<string, MtComponentAttribute>
{
    /// <summary>
    /// Checks the attribute list for if-condition and if found removes & returns it, else null.
    /// </summary>
    public string? PullIfCondition()
    {
        return ContainsKey("if") ? Pull("if") : null;
    }

    /// <summary>
    /// Checks the attribute list for loop-condition and if found removes & returns it, else null.
    /// </summary>
    public MtLoop? PullLoop(MtDataContext context, int nodeId, int depth)
    {
        return ContainsKey("foreach")
            ? MtForeach.FromString(Pull("foreach"), context, nodeId, depth)
            : null;
    }

    /// <summary>
    /// Checks the attribute list for name and if found removes & returns it, else "default".
    /// </summary>
    public string PullName()
    {
        return ContainsKey("name") ? Pull("name") : "default";
    }

    public string Pull(string name)
    {
        var value = this[name];
        Remove(name);
        return value.Value;
    }
}