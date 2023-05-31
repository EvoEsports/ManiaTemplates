namespace ManiaTemplates.Exceptions;

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