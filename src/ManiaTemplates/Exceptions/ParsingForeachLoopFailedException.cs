namespace ManiaTemplates.Exceptions;

public class ParsingForeachLoopFailedException : Exception
{
    public ParsingForeachLoopFailedException()
    {
    }

    public ParsingForeachLoopFailedException(string message)
        : base(message)
    {
    }

    public ParsingForeachLoopFailedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}