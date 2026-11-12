namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when an import or property node of a component is missing a required attribute.
/// </summary>
public class MissingAttributeException : Exception
{
    public MissingAttributeException()
    {
    }

    public MissingAttributeException(string message)
        : base(message)
    {
    }

    public MissingAttributeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}