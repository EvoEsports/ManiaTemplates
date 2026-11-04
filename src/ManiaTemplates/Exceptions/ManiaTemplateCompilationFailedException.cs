namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when the pre-processed template fails to compile or load into its load context.
/// </summary>
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
