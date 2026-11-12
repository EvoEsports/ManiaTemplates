using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.IntegrationTests;

public class XmlAttributeEscaperTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();

    [Fact]
    public void Should_Flip_Mapping()
    {
        var mapping = new Dictionary<string, string>
        {
            { "one", "test1" },
            { "two", "test2" },
        };

        var flipped = XmlAttributeEscaper.FlipMapping(mapping);
        Assert.Equal("one", flipped["test1"]);
        Assert.Equal("two", flipped["test2"]);
    }

    [Fact]
    public void Should_Substitutes_Elements_In_String()
    {
        var mapping = new Dictionary<string, string>
        {
            { "match1", "Unit" },
            { "match2", "test" },
        };

        var processed = XmlAttributeEscaper.SubstituteStrings("Hello match1 this is a match2.", mapping);
        Assert.Equal("Hello Unit this is a test.", processed);
    }

    [Fact]
    public void Should_Find_And_Escape_Attributes_In_Xml_Node()
    {
        var processedSingleQuotes = XmlAttributeEscaper.FindAndEscapeAttributes("<SomeNode some-attribute='test&'/>", XmlAttributeEscaper.XmlTagAttributeMatcherSingleQuote);
        Assert.Equal("<SomeNode some-attribute='test&amp;'/>", processedSingleQuotes);
        
        var processedDoubleQuotes = XmlAttributeEscaper.FindAndEscapeAttributes("<SomeNode attribute=\"test>\"/>", XmlAttributeEscaper.XmlTagAttributeMatcherDoubleQuote);
        Assert.Equal("<SomeNode attribute=\"test&gt;\"/>", processedDoubleQuotes);
    }

    [Theory]
    [InlineData("<SomeNode empty=\"\" other=\"a < b\"/>", "<SomeNode empty=\"\" other=\"a &lt; b\"/>")]
    [InlineData("<SomeNode empty='' other='a < b'/>", "<SomeNode empty='' other='a &lt; b'/>")]
    [InlineData("<SomeNode attribute=\"<\"/>", "<SomeNode attribute=\"&lt;\"/>")]
    [InlineData("<SomeNode one=\"&amp; &\" two=\"it's\"/>", "<SomeNode one=\"&amp; &amp;\" two=\"it&apos;s\"/>")]
    [InlineData("<a x=\"1 > 0\"/><b x=\"1 > 0\"/><c x=\"\"/>", "<a x=\"1 &gt; 0\"/><b x=\"1 &gt; 0\"/><c x=\"\"/>")]
    public void Should_Only_Escape_Attribute_Values(string input, string expected)
    {
        Assert.Equal(expected, XmlAttributeEscaper.EscapeXmlSpecialCharsInAttributes(input));
    }

    [Fact]
    public async Task Should_Escape_Special_Chars_In_Attributes()
    {
        var escapeTestComponent = await File.ReadAllTextAsync("IntegrationTests/templates/escape-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/escape-test.xml");

        _maniaTemplateEngine.AddTemplateFromString("EscapeTest", escapeTestComponent);

        var template = _maniaTemplateEngine.RenderAsync("EscapeTest", new
        {
            data = Enumerable.Range(0, 4).ToList()
        }).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }
}