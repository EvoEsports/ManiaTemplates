namespace ManiaTemplates.Exceptions;

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