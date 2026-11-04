namespace ManiaTemplates.Exceptions;

/// <summary>
/// Thrown when an interpolation contains another interpolation, like {{ {{ value }} }}.
/// </summary>
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