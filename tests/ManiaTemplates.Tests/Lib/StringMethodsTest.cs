using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Tests.Lib;

public class StringMethodsTest
{
    [Theory]
    [InlineData("string", true)]
    [InlineData("string?", true)]
    [InlineData("String", true)]
    [InlineData("System.String", true)]
    [InlineData("global::System.String", true)]
    [InlineData("List<string>", false)]
    [InlineData("Dictionary<string, int>", false)]
    [InlineData("StringBuilder", false)]
    [InlineData("string[]", false)]
    [InlineData("int", false)]
    public void Should_Detect_String_Type(string type, bool expected)
    {
        Assert.Equal(expected, IStringMethods.IsStringType(type));
    }
}
