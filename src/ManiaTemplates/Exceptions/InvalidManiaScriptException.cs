namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a script tag contains other nodes or the script contains '--', which would end the surrounding XML comment.
/// </summary>
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