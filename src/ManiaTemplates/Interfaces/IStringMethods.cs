using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

public interface IStringMethods
{
    /// <summary>
    /// Wrap the second argument in quotes, if the given property is a string type.
    /// </summary>
    public static string WrapIfString(MtComponentProperty property, string value)
    {
        return property.IsStringType() ? WrapStringInQuotes(value) : value;
    }

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    public static string WrapStringInQuotes(string str)
    {
        return $@"$""{str}""";
    }
}