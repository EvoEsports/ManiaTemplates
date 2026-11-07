using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.Components;

public class MtComponentScriptTest
{
    private readonly ManiaTemplateEngine _templateEngine = new();

    public MtComponentScriptTest()
    {
        _templateEngine.AddManiaScriptFromString("res", "text");
    }

    [Theory]
    [InlineData("<script>text</script>", false, false)]
    [InlineData("<script once=''>text</script>", false, true)]
    [InlineData("<script resource='res'/>", false, false)]
    [InlineData("<script resource='res' once=''/>", false, true)]
    public void Should_Load_ManiaScript(string input, bool expectedMain, bool expectedOnce)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        var res = MtComponentScript.FromNode(_templateEngine, document.DocumentElement!);
        Assert.Equal("text", res.Content);
        Assert.Equal(expectedMain, res.HasMainMethod);
        Assert.Equal(expectedOnce, res.Once);
    }

    [Theory]
    [InlineData("<script>main(){ //dummy comment }</script>", true, false)]
    [InlineData("<script once=''>main(){ //dummy comment }</script>", true, true)]
    public void Should_Load_ManiaScriptMainMethod(string input, bool expectedMain, bool expectedOnce)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        var res = MtComponentScript.FromNode(_templateEngine, document.DocumentElement!);
        Assert.Equal("main(){ //dummy comment }", res.Content);
        Assert.Equal(expectedMain, res.HasMainMethod);
        Assert.Equal(expectedOnce, res.Once);
    }

    [Theory]
    [InlineData("<script/>")]
    [InlineData("<script once='true' />")]
    public void Should_Fail_To_Load_Empty_ManiaScript(string input)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        Assert.Throws<ManiaScriptSourceMissingException>(() =>
            MtComponentScript.FromNode(_templateEngine, document.DocumentElement!));
    }

    [Fact]
    public void Should_Load_ManiaScript_With_Different_Hash_Codes()
    {
        const string firstInput = "<script>a</script>";
        var document1 = new XmlDocument();
        document1.LoadXml(firstInput);
        var script1 = MtComponentScript.FromNode(_templateEngine, document1.DocumentElement!);
        var script1Hash = script1.ContentHash();

        const string secondInput = "<script resource='res'/>";
        var document2 = new XmlDocument();
        document2.LoadXml(secondInput);
        var script2 = MtComponentScript.FromNode(_templateEngine, document2.DocumentElement!);
        var script2Hash = script2.ContentHash();

        Assert.NotEqual(script1Hash, script2Hash);
    }

    [Theory]
    [InlineData("<script>log(1 &lt;=&gt; 2);</script>")]
    [InlineData("<script><!--log(1 <=> 2);--></script>")]
    [InlineData("<script><!--log(1 <=>--> <!-- 2);--></script>")]
    [InlineData("<script><![CDATA[log(1 <=> 2);]]></script>")]
    public void Should_Read_Inline_ManiaScript_Without_Wrapper(string input)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        var script = MtComponentScript.FromNode(_templateEngine, document.DocumentElement!);

        Assert.Equal("log(1 <=> 2);", script.Content);
    }

    [Theory]
    [InlineData("log(1 <=> 2);", "log(1 <=> 2);")]
    [InlineData("log(a && b);", "log(a && b);")]
    [InlineData("log(1 &lt; 2);", "log(1 < 2);")]
    [InlineData("\uFEFF<!--\nlog(1 <=> 2);\n-->", "\nlog(1 <=> 2);\n")]
    [InlineData("<!-- A -->\n</script>\n<script>\n<!-- B -->", " A \n B ")]
    [InlineData("declare Text T = \"<b>bold</b>\";", "declare Text T = \"<b>bold</b>\";")]
    public void Should_Read_Resource_ManiaScript_Without_Wrapper(string resource, string expected)
    {
        _templateEngine.AddManiaScriptFromString("resource", resource);
        var document = new XmlDocument();
        document.LoadXml("<script resource='resource'/>");

        var script = MtComponentScript.FromNode(_templateEngine, document.DocumentElement!);

        Assert.Equal(expected, script.Content);
    }

    [Theory]
    [InlineData("<script>a -- b</script>")]
    [InlineData("<script resource='invalid'/>")]
    [InlineData("<script resource='invalidRaw'/>")]
    [InlineData("<script><label/></script>")]
    public void Should_Reject_Invalid_ManiaScript(string input)
    {
        _templateEngine.AddManiaScriptFromString("invalid", "<!-- a -- b -->");
        _templateEngine.AddManiaScriptFromString("invalidRaw", "if (a < b) { c -- d; }");
        var document = new XmlDocument();
        document.LoadXml(input);

        Assert.Throws<InvalidManiaScriptException>(() =>
            MtComponentScript.FromNode(_templateEngine, document.DocumentElement!));
    }
}
