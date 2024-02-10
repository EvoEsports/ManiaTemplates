using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Components;

public class MtComponentProperty : IStringMethods
{
    public required string Type { get; init; }
    public required string Name { get; init; }
    public string? Default { get; init; }

    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    public bool IsStringType() => IStringMethods.IsStringType(Type);

    /// <summary>
    /// Gets the default value for this property and wraps it in quotes, if string type.
    /// </summary>
    public string? GetDefaultWrapped()
    {
        if (Default == null)
        {
            return null;
        }

        return IsStringType() ? IStringMethods.WrapStringInQuotes(Default) : Default;
    }

    /// <summary>
    /// Converts the property to method argument format.
    /// </summary>
    public string ToMethodArgument()
    {
        return Default == null
            ? $"{Type} {Name}"
            : $"{Type} {Name} = {GetDefaultWrapped()}";
    }
}