namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a for attribute is not a valid for loop or declares invalid variables.
/// </summary>
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