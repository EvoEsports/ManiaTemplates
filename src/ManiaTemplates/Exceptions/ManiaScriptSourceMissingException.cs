namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a script tag has neither content nor a resource attribute.
/// </summary>
public class ManiaScriptSourceMissingException : Exception
{
    public ManiaScriptSourceMissingException()
    {
    }

    public ManiaScriptSourceMissingException(string message)
        : base(message)
    {
    }

    public ManiaScriptSourceMissingException(string message, Exception inner)
        : base(message, inner)
    {
    }
}