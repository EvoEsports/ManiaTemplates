﻿using ManiaTemplates.ControlElements;

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
    /// Checks the attribute list for loop-condition and returns it, else null.
    /// </summary>
    public MtForeach? PullForeachCondition(MtDataContext context, int nodeId, int loopDepth)
    {
        return ContainsKey("foreach")
            ? MtForeach.FromString(Pull("foreach"), context, nodeId, loopDepth)
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