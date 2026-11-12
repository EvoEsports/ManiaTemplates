namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when the opening and closing curly braces of an interpolation do not match.
/// </summary>
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