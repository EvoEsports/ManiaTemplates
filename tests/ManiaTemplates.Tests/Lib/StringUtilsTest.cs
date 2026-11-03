using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class StringUtilsTest
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
        Assert.Equal(expected, StringUtils.IsStringType(type));
    }

    [Fact]
    public void Should_Wrap_Strings_In_Quotes()
    {
        Assert.Equal(@"$""unit test""", StringUtils.WrapStringInQuotes("unit test"));
        Assert.Equal(@"$""""", StringUtils.WrapStringInQuotes(""));
    }
}
