namespace ManiaTemplates.Exceptions;

public class ManiaScriptAlreadyExistsException : Exception
{
    public ManiaScriptAlreadyExistsException()
    {
    }

    public ManiaScriptAlreadyExistsException(string message)
        : base(message)
    {
    }

    public ManiaScriptAlreadyExistsException(string message, Exception inner)
        : base(message, inner)
    {
    }
}