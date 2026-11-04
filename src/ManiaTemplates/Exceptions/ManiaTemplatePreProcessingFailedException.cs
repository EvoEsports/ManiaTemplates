namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when T4 fails to pre-process the template generated from a component.
/// </summary>
public class ManiaTemplatePreProcessingFailedException: Exception
{
    public ManiaTemplatePreProcessingFailedException()
    {
    }

    public ManiaTemplatePreProcessingFailedException(string message)
        : base(message)
    {
    }

    public ManiaTemplatePreProcessingFailedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}