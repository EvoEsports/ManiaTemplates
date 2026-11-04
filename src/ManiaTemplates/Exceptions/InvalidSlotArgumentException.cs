namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when the arguments a slot passes to its contents are invalid or differ between positions of the slot.
/// </summary>
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