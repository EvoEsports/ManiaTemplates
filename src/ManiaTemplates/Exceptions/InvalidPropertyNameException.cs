namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a component property name is not a valid C# variable name, is a C# keyword or starts with __.
/// </summary>
public class InvalidPropertyNameException : Exception
{
    public InvalidPropertyNameException()
    {
    }

    public InvalidPropertyNameException(string message)
        : base(message)
    {
    }

    public InvalidPropertyNameException(string message, Exception inner)
        : base(message, inner)
    {
    }
}