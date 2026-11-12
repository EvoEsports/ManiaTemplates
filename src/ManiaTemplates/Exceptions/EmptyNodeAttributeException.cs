namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when an attribute that requires a value, like a condition or a slot name, is empty.
/// </summary>
public class EmptyNodeAttributeException : Exception
{
    public EmptyNodeAttributeException()
    {
    }

    public EmptyNodeAttributeException(string message) : base(message)
    {
    }

    public EmptyNodeAttributeException(string message, Exception inner) : base(message, inner)
    {
    }
}