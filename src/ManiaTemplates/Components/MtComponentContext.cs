﻿namespace ManiaTemplates.Components;

public class MtComponentContext : Dictionary<string, string>
{
    public MtComponentContext? ParentContext { get; init; }

    public MtComponentContext NewContext(MtComponentContext otherContext)
    {
        var clone = new MtComponentContext { ParentContext = this };

        foreach (var (name, type) in otherContext)
        {
            clone[name] = type;
        }

        return clone;
    }

    public string ToString()
    {
        return "Context" + GetHashCode().ToString().Replace("-", "N");
    }
}