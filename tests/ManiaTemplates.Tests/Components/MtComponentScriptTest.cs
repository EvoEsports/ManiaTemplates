﻿using System.Xml;
using ManiaTemplates.Components;

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
    [InlineData("<script main=''>text</script>", true, false)]
    [InlineData("<script once=''>text</script>", false, true)]
    [InlineData("<script main='' once=''>text</script>", true, true)]
    [InlineData("<script resource='res'/>", false, false)]
    [InlineData("<script resource='res' main=''/>", true, false)]
    [InlineData("<script resource='res' once=''/>", false, true)]
    [InlineData("<script resource='res' main='' once=''/>", true, true)]
    public void Should_Load_ManiaScript(string input, bool expectedMain, bool expectedOnce)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        var res = MtComponentScript.FromNode(_templateEngine, document.DocumentElement!);
        Assert.Equal("text", res.Content);
        Assert.Equal(expectedMain, res.Main);
        Assert.Equal(expectedOnce, res.Once);
    }

    [Theory]
    [InlineData("<script/>")]
    [InlineData("<script main='false' once='true' />")]
    public void Should_Fail_To_Load_Empty_ManiaScript(string input)
    {
        var document = new XmlDocument();
        document.LoadXml(input);

        var exception = Assert.Throws<Exception>(() => MtComponentScript.FromNode(_templateEngine, document.DocumentElement!));
        Assert.Equal("Failed to get ManiaScript contents. Script tags need to either specify a body or resource-attribute.", exception.Message);
    }

    [Fact]
    public void Should_Load_ManiaScript_With_Different_Hash_Codes()
    {
        const string firstInput = "<script>a</script>";
        var document1 = new XmlDocument();
        document1.LoadXml(firstInput);
        var script1 = MtComponentScript.FromNode(_templateEngine, document1.DocumentElement!);
        
        const string secondInput = "<script resource='res'/>";
        var document2 = new XmlDocument();
        document2.LoadXml(secondInput);
        var script2 = MtComponentScript.FromNode(_templateEngine, document2.DocumentElement!);
        
        Assert.NotEqual(script1, script2);
        Assert.NotEqual(script1.ContentHash(), script2.ContentHash());
    }
}
