namespace ManiaTemplates.Exceptions;

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