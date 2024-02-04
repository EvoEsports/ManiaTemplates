namespace ManiaTemplates.Exceptions;

public class CurlyBraceCountMismatchException : Exception
{
    public CurlyBraceCountMismatchException()
    {
    }

    public CurlyBraceCountMismatchException(string message) : base(message)
    {
    }

    public CurlyBraceCountMismatchException(string message, Exception inner) : base(message, inner)
    {
    }
}