namespace ManiaTemplates.Exceptions;

public class MissingComponentRootException : Exception
{
    public MissingComponentRootException()
    {
    }

    public MissingComponentRootException(string message)
        : base(message)
    {
    }

    public MissingComponentRootException(string message, Exception inner)
        : base(message, inner)
    {
    }
}