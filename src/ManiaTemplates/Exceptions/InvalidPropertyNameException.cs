namespace ManiaTemplates.Exceptions;

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