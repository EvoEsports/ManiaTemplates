namespace ManiaTemplates.Exceptions;

public class ManiaScriptBodyInvalidSequenceException : Exception
{
    public ManiaScriptBodyInvalidSequenceException()
    {
    }

    public ManiaScriptBodyInvalidSequenceException(string message)
        : base(message)
    {
    }

    public ManiaScriptBodyInvalidSequenceException(string message, Exception inner)
        : base(message, inner)
    {
    }
}