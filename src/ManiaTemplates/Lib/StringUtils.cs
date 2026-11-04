using Microsoft.CodeAnalysis.CSharp;

namespace ManiaTemplates.Lib;

/// <summary>
/// Helpers for C# names and values in generated code.
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    /// <param name="typeString">The C# type.</param>
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
    /// <param name="name">The name to check.</param>
    public static bool IsVariableName(string name) =>
        SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None &&
        !name.StartsWith("__");

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    /// <param name="str">The string to wrap.</param>
    public static string WrapStringInQuotes(string str)
        => $@"$""{str}""";
}