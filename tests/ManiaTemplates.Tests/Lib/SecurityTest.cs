using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class SecurityTest
{
    [Fact]
    public void Should_Escape_Xml_Characters()
    {
        Assert.Equal("&lt;", Security.Escape("<"));
        Assert.Equal("&gt;", Security.Escape(">"));
        Assert.Equal("&quot;", Security.Escape("\""));
        Assert.Equal("&apos;", Security.Escape("'"));
        Assert.Equal("&amp;", Security.Escape("&"));
    }
    
    [Fact]
    public void Should_Escape_Double_Minus()
    {
        Assert.Equal("unit&#45;&#45;test", Security.Escape("unit--test"));
    }
}