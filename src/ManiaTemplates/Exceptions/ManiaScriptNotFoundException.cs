namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a script references a ManiaScript resource that is not registered.
/// </summary>
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