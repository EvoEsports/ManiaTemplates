using Microsoft.CodeAnalysis.CSharp;

namespace ManiaTemplates.Interfaces;

public interface IStringMethods
{
    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    public static bool IsStringType(string typeString)
    {
        var type = typeString.Replace(" ", "").TrimEnd('?');
        if (type.StartsWith("global::"))
        {
            type = type["global::".Length..];
        }

        return type is "string" or "String" or "System.String";
    }

    /// <summary>
    /// Checks whether the name can be used as variable of the generated code: a valid identifier, not a C# keyword and
    /// not starting with __, as these names are used for internal variables.
    /// </summary>
    public static bool IsVariableName(string name) =>
        SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None &&
        !name.StartsWith("__");

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    public static string WrapStringInQuotes(string str)
        => $@"$""{str}""";
}