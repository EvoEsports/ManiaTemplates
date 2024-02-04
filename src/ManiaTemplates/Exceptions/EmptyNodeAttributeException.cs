namespace ManiaTemplates.Exceptions;

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