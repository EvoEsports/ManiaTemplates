namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when a template has no <c>&lt;component&gt;</c> root node.
/// </summary>
public class MissingComponentRootException : Exception
{
    public MissingComponentRootException()
    {
    }

    public MissingComponentRootException(string message)
        : base(message)
    {
    }

    public MissingComponentRootException(string message, Exception inner)
        : base(message, inner)
    {
    }
}