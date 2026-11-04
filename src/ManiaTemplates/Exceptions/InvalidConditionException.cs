namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when the if, else-if and else attributes are combined incorrectly, e.g. else without a preceding if.
/// </summary>
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