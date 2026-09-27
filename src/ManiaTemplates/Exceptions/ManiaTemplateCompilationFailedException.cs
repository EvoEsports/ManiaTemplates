namespace ManiaTemplates.Exceptions;

public class ManiaTemplateCompilationFailedException : Exception
{
    public ManiaTemplateCompilationFailedException()
    {
    }

    public ManiaTemplateCompilationFailedException(string message)
        : base(message)
    {
    }

    public ManiaTemplateCompilationFailedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
