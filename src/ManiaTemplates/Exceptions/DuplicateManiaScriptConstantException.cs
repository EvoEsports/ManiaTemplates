namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when scripts of a manialink define the same #Const with different values.
/// </summary>
public class DuplicateManiaScriptConstantException : Exception
{
    public DuplicateManiaScriptConstantException()
    {
    }

    public DuplicateManiaScriptConstantException(string message)
        : base(message)
    {
    }

    public DuplicateManiaScriptConstantException(string message, Exception inner)
        : base(message, inner)
    {
    }
}