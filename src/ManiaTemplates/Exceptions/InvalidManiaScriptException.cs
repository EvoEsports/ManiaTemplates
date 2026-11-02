namespace ManiaTemplates.Exceptions;

public class InvalidManiaScriptException : Exception
{
    public InvalidManiaScriptException()
    {
    }

    public InvalidManiaScriptException(string message)
        : base(message)
    {
    }

    public InvalidManiaScriptException(string message, Exception inner)
        : base(message, inner)
    {
    }
}