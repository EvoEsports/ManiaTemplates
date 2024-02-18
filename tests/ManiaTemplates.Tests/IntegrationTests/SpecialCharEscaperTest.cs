using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.IntegrationTests;

public class SpecialCharEscaperTest
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

        var flipped = MtSpecialCharEscaper.FlipMapping(mapping);
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

        var processed = MtSpecialCharEscaper.SubstituteStrings("Hello match1 this is a match2.", mapping);
        Assert.Equal("Hello Unit this is a test.", processed);
    }

    [Fact]
    public void Should_Find_And_Escape_Attributes_In_Xml_Node()
    {
        var processedSingleQuotes = MtSpecialCharEscaper.FindAndEscapeAttributes("<SomeNode some-attribute='test&'/>", MtSpecialCharEscaper.XmlTagAttributeMatcherSingleQuote);
        Assert.Equal("<SomeNode some-attribute='test&amp;'/>", processedSingleQuotes);
        
        var processedDoubleQuotes = MtSpecialCharEscaper.FindAndEscapeAttributes("<SomeNode attribute=\"test>\"/>", MtSpecialCharEscaper.XmlTagAttributeMatcherDoubleQuote);
        Assert.Equal("<SomeNode attribute=\"test&gt;\"/>", processedDoubleQuotes);
    }

    [Fact]
    public async void Should_Escape_Special_Chars_In_Attributes()
    {
        var escapeTestComponent = await File.ReadAllTextAsync("IntegrationTests/templates/escape-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/escape-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("EscapeTest", escapeTestComponent);

        var template = _maniaTemplateEngine.RenderAsync("EscapeTest", new
        {
            data = Enumerable.Range(0, 4).ToList()
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }
}