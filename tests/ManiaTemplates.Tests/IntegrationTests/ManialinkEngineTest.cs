namespace ManiaTemplates.Tests.IntegrationTests;

public class ManialinkEngineTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();
    
    [Theory]
    [ClassData(typeof(TestDataProvider))]
    public void ShouldConvertTemplatesToResult(string template, dynamic data, string expected)
    {
        _maniaTemplateEngine.AddTemplateFromString("test", template);
        _maniaTemplateEngine.PreProcess("test", new[] { typeof(ManiaTemplateEngine).Assembly });
        var result = _maniaTemplateEngine.Render("test", data, new[] { typeof(ManiaTemplateEngine).Assembly });
        
        Assert.Equal(expected, result, ignoreWhiteSpaceDifferences: true);
    }
}
