namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a node has an attribute that is not supported in its position, like conditions on slot templates.
/// </summary>
public class UnsupportedAttributeException : Exception
{
    public UnsupportedAttributeException()
    {
    }

    public UnsupportedAttributeException(string message)
        : base(message)
    {
    }

    public UnsupportedAttributeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}