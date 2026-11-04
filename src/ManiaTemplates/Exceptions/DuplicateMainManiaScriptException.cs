namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when more than one distinct script of a manialink defines a main method.
/// </summary>
public class DuplicateMainManiaScriptException : Exception
{
    public DuplicateMainManiaScriptException()
    {
    }

    public DuplicateMainManiaScriptException(string message)
        : base(message)
    {
    }

    public DuplicateMainManiaScriptException(string message, Exception inner)
        : base(message, inner)
    {
    }
}