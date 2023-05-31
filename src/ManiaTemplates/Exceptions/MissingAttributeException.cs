namespace ManiaTemplates.Exceptions;

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