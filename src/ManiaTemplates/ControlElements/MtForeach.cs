using System.Text.RegularExpressions;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.ControlElements;

public class MtForeach
{
    public required string Condition { get; init; }
    public required Dictionary<string, string> Variables { get; init; }

    private static readonly Regex ForeachConditionRegex =
        new(@"^(?:ref\s+)?(?:readonly\s+)?(?:(T|V|var|[a-zA-Z].*?)\s+)?(.+?)\s+in\s(.+)$");

    private static readonly Regex ForeachVariablesRegex = new(@"^\(?(.+?)(?:$|,\s*(.+)\))");
    private static readonly Regex ForeachVariablesTypeSplitterRegex = new(@"^(.+?)(?:$|\s+(.+)$)");

    public static MtForeach FromString(string foreachAttributeValue)
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

        var foundVariables = new Dictionary<string, string>();
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

            var typeOrVariable = variableAndType.Groups[1];
            var variableOrEmpty = variableAndType.Groups[2];

            if (variableOrEmpty.Length == 0)
            {
                // Console.WriteLine($"add: {typeOrVariable.Value} -> {type}");
                foundVariables.Add(typeOrVariable.Value, type);
            }
            else
            {
                if (typeOrVariable.Value == "var")
                {
                    throw new ParsingForeachLoopFailedException("You may not use var in foreach loops, please specify type.");
                }
                
                // Console.WriteLine($"add: {variableOrEmpty.Value} -> {typeOrVariable.Value}");
                foundVariables.Add(variableOrEmpty.Value, typeOrVariable.Value);
            }
        }

        //Prevent user defining __index variable
        if (foundVariables.Keys.Any(variableName => variableName.StartsWith("__")))
        {
            throw new ParsingForeachLoopFailedException("User defined variables must not start with __.");
        }

        //Add __index now
        foundVariables.Add("__index", "int");

        return new MtForeach
        {
            Condition = foreachAttributeValue,
            Variables = foundVariables
        };
    }
}