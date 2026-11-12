namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when scripts of a manialink include different libraries under the same alias.
/// </summary>
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