namespace ManiaTemplates.Exceptions;

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