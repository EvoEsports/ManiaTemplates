namespace ManiaTemplates.Interfaces;

public interface IStringMethods
{
    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    public static bool IsStringType(string typeString)
        => typeString.ToLower().Contains("string"); //TODO: find better way to determine string

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    public static string WrapStringInQuotes(string str)
        => $@"$""{str}""";
}