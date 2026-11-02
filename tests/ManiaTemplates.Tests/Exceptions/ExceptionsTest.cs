namespace ManiaTemplates.Tests.Exceptions;

public class ExceptionsTest
{
    public static IEnumerable<object[]> ExceptionTypes() =>
        typeof(ManiaTemplateEngine).Assembly.GetTypes()
            .Where(type => type.Namespace == "ManiaTemplates.Exceptions" && typeof(Exception).IsAssignableFrom(type))
            .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(ExceptionTypes))]
    public void Should_Provide_Standard_Exception_Constructors(Type exceptionType)
    {
        var inner = new InvalidOperationException("inner");

        var withoutMessage = (Exception)Activator.CreateInstance(exceptionType)!;
        var withMessage = (Exception)Activator.CreateInstance(exceptionType, "message")!;
        var withInner = (Exception)Activator.CreateInstance(exceptionType, "message", inner)!;

        Assert.Null(withoutMessage.InnerException);
        Assert.Equal("message", withMessage.Message);
        Assert.Equal("message", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
    }
}
