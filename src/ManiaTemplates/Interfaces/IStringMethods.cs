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
    /// Wraps a string in quotes.
    /// </summary>
    public static string WrapStringInQuotes(string str)
        => $@"$""{str}""";
}