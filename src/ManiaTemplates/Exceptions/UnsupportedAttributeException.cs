namespace ManiaTemplates.Exceptions;

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