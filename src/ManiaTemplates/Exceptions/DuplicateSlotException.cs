namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a component usage contains multiple templates filling the same named slot.
/// </summary>
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