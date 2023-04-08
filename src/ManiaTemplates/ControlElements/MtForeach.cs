using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.ControlElements;

public class MtForeach
{
    public required string Condition { get; init; }
    public required List<MtForeachVariable> Variables { get; init; }
    public required MtDataContext Context { get; init; }

    private static readonly Regex ForeachConditionRegex =
        new(@"^(?:ref\s+)?(?:readonly\s+)?(?:(T|V|var|[a-zA-Z].*?)\s+)?(.+?)\s+in\s(.+)$");

    private static readonly Regex ForeachVariablesRegex = new(@"^\(?(.+?)(?:$|,\s*(.+)\))");
    private static readonly Regex ForeachVariablesTypeSplitterRegex = new(@"^(.+?)(?:$|\s+(.+)$)");

    public static MtForeach FromString(string foreachAttributeValue, MtDataContext context, int nodeId)
    {
        //Match the value of the foreach-attribute of the XmlNode.
        //Split it into type, variables (var x, var (x,y), ...) and the source.
        var conditionMatch = ForeachConditionRegex.Match(foreachAttributeValue);
        if (!conditionMatch.Success)
        {
            throw new ParsingForeachLoopFailedException("Failed to parse foreach condition.");
        }

        var type = conditionMatch.Groups[1].Value;
        var variables = conditionMatch.Groups[2].Value;
        var sourceEnumerable = conditionMatch.Groups[3].Value;

        if (type == "var")
        {
            throw new ParsingForeachLoopFailedException("You may not use var in foreach loops, please specify type.");
        }

        // Console.WriteLine($"type -> {type}");
        // Console.WriteLine($"variables -> {variables}");
        // Console.WriteLine($"sourceEnumerable -> {sourceEnumerable}\n");

        //Split the variables if more than one is defined
        var variablesMatch = ForeachVariablesRegex.Match(variables);
        if (!variablesMatch.Success || variablesMatch.Groups.Count < 3)
        {
            throw new ParsingForeachLoopFailedException("Failed to parse variables of foreach condition.");
        }

        var foundVariables = new List<MtForeachVariable>();
        foreach (var group in variablesMatch.Groups.Values.Skip(1))
        {
            if (group.Length == 0) continue;

            //Parse the variable types and names of the loop
            var variableAndType = ForeachVariablesTypeSplitterRegex.Match(group.Value);
            if (!variableAndType.Success)
            {
                throw new ParsingForeachLoopFailedException(
                    "Failed to split variables and types of foreach condition.");
            }

            var typeOrName = variableAndType.Groups[1];
            var nameOrEmpty = variableAndType.Groups[2];

            if (nameOrEmpty.Length == 0)
            {
                // Console.WriteLine($"add: {typeOrVariable.Value} -> {type}");
                foundVariables.Add(new MtForeachVariable
                {
                    Type = type,
                    Name = typeOrName.Value
                });
            }
            else
            {
                if (typeOrName.Value == "var")
                {
                    throw new ParsingForeachLoopFailedException(
                        "You may not use var in foreach loops, please specify type.");
                }

                // Console.WriteLine($"add: {variableOrEmpty.Value} -> {typeOrVariable.Value}");
                foundVariables.Add(new MtForeachVariable
                {
                    Type = typeOrName.Value,
                    Name = nameOrEmpty.Value
                });
            }
        }

        //Prevent user defining __index variable
        if (foundVariables.Any(variable => variable.Name.StartsWith("__")))
        {
            throw new ParsingForeachLoopFailedException("User defined variables must not start with __.");
        }

        var newContext = new MtDataContext($"ForEachLoop{nodeId}")
        {
            { "__index", "int" }
        };

        foreach (var variable in foundVariables)
        {
            newContext[variable.Name] = variable.Type;
        }

        return new MtForeach
        {
            Condition = foreachAttributeValue,
            Variables = foundVariables,
            Context = context.NewContext(newContext)
        };
    }
}