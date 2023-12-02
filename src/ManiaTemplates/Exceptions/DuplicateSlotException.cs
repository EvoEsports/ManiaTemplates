namespace ManiaTemplates.Exceptions;

public class DuplicateSlotException : Exception
{
    public DuplicateSlotException()
    {
    }

    public DuplicateSlotException(string message)
        : base(message)
    {
    }

    public DuplicateSlotException(string message, Exception inner)
        : base(message, inner)
    {
    }
}