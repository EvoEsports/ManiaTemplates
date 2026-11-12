namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when adding a ManiaScript with a key that is already registered.
/// </summary>
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