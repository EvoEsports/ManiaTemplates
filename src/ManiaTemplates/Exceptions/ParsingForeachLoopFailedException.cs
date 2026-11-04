namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a foreach attribute is not a valid foreach loop or declares invalid variables.
/// </summary>
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