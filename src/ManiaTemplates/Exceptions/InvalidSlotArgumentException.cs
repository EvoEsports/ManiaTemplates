namespace ManiaTemplates.Exceptions;

public class InvalidSlotArgumentException : Exception
{
    public InvalidSlotArgumentException()
    {
    }

    public InvalidSlotArgumentException(string message)
        : base(message)
    {
    }

    public InvalidSlotArgumentException(string message, Exception inner)
        : base(message, inner)
    {
    }
}