namespace ManiaTemplates.Exceptions;

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