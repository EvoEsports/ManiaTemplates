namespace ManiaTemplates.Exceptions;

public class DuplicateManiaScriptIncludeException : Exception
{
    public DuplicateManiaScriptIncludeException()
    {
    }

    public DuplicateManiaScriptIncludeException(string message)
        : base(message)
    {
    }

    public DuplicateManiaScriptIncludeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}