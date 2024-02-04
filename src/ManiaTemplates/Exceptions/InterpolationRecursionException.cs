namespace ManiaTemplates.Exceptions;

public class InterpolationRecursionException : Exception
{
    public InterpolationRecursionException()
    {
    }

    public InterpolationRecursionException(string message) : base(message)
    {
    }

    public InterpolationRecursionException(string message, Exception inner) : base(message, inner)
    {
    }
}