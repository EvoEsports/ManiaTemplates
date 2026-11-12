namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when scripts of a manialink define structs with the same name but different fields.
/// </summary>
public class DuplicateManiaScriptStructException : Exception
{
    public DuplicateManiaScriptStructException()
    {
    }

    public DuplicateManiaScriptStructException(string message)
        : base(message)
    {
    }

    public DuplicateManiaScriptStructException(string message, Exception inner)
        : base(message, inner)
    {
    }
}