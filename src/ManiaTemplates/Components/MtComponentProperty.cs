using ManiaTemplates.Lib;

namespace ManiaTemplates.Components;

/// <summary>
/// A property of a component, which is set with an attribute of the same name where the component is used.
/// </summary>
public class MtComponentProperty
{
    /// <summary>
    /// The C# type of the property.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The name of the property and of the attribute setting it.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The default value used when the attribute is not set, the property is required if null.
    /// </summary>
    public string? Default { get; init; }

    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    public bool IsStringType() => StringUtils.IsStringType(Type);

    /// <summary>
    /// Gets the default value for this property and wraps it in quotes, if string type.
    /// </summary>
    public string? GetDefaultWrapped()
    {
        if (Default == null)
        {
            return null;
        }

        return IsStringType() ? StringUtils.WrapStringInQuotes(Default) : Default;
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

    /// <summary>
    /// Converts the property to constant for use in render methods.
    /// </summary>
    public string ToLocalConstant()
    {
        return $"const {ToMethodArgument()};";
    }
}