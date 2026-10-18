namespace ManiaTemplates.Exceptions;

public class ParsingForLoopFailedException : Exception
{
    public ParsingForLoopFailedException()
    {
    }

    public ParsingForLoopFailedException(string message)
        : base(message)
    {
    }

    public ParsingForLoopFailedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}