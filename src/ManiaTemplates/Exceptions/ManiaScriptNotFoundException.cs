namespace ManiaTemplates.Exceptions;

public class ManiaScriptNotFoundException : Exception
{
    public ManiaScriptNotFoundException()
    {
    }

    public ManiaScriptNotFoundException(string message)
        : base(message)
    {
    }

    public ManiaScriptNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}