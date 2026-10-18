namespace ManiaTemplates.Exceptions;

public class InvalidConditionException : Exception
{
    public InvalidConditionException()
    {
    }

    public InvalidConditionException(string message)
        : base(message)
    {
    }

    public InvalidConditionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}