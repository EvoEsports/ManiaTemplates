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

        Assert.NotEqual(script1, script2);
        Assert.NotEqual(script1Hash, script2Hash);
    }
}