using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class MtEscaperTest
{
    [Fact]
    public void Should_Escape_Xml_Characters()
    {
        Assert.Equal("&lt;", MtEscaper.Escape("<"));
        Assert.Equal("&gt;", MtEscaper.Escape(">"));
        Assert.Equal("&quot;", MtEscaper.Escape("\""));
        Assert.Equal("&apos;", MtEscaper.Escape("'"));
        Assert.Equal("&amp;", MtEscaper.Escape("&"));
        Assert.Equal("unit&#45;&#45;test", MtEscaper.Escape("unit--test"));
    }
}