namespace ManiaTemplates.ControlElements;

public class MtContextAlias
{
    public MtDataContext Context { get; init; }
    public Dictionary<string, string> Aliases { get; } = new();

    public MtContextAlias(MtDataContext context)
    {
        Context = context;
        foreach (var (variableName, variableType) in Context)
        {
            var variableAlias = variableName + (new Random()).Next();
            Aliases[variableName] = variableAlias;
        }
    }
}