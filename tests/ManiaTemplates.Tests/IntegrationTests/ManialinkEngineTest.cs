namespace ManiaTemplates.Tests.IntegrationTests;

public class ManialinkEngineTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();
    
    [Theory]
    [ClassData(typeof(TestDataProvider))]
    public void Should_Convert_Templates_To_Result(string template, dynamic data, string expected)
    {
        _maniaTemplateEngine.AddTemplateFromString("test", template);
        _maniaTemplateEngine.PreProcess("test", new[] { typeof(ManiaTemplateEngine).Assembly });
        
        var pendingResult = _maniaTemplateEngine.RenderAsync("test", data, new[] { typeof(ManiaTemplateEngine).Assembly });
        var result = pendingResult.Result;
        
        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Pass_Global_Variables()
    {
        var componentTemplate = File.ReadAllText($"IntegrationTests/templates/global-variables.mt");
        var expectedOutput = File.ReadAllText($"IntegrationTests/expected/global-variables.xml");
        
        _maniaTemplateEngine.AddTemplateFromString("GlobalVariables", componentTemplate);

        var complexVar = new ComplexDataType();
        _maniaTemplateEngine.SetGlobalVariable("testVariable", "unittest");
        _maniaTemplateEngine.SetGlobalVariable("complex", complexVar);
        
        //TODO: auto pre-process on var add/remove
        _maniaTemplateEngine.PreProcess("GlobalVariables", new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly });
        
        var pendingResult = _maniaTemplateEngine.RenderAsync("GlobalVariables", new{}, new[] { typeof(ManiaTemplateEngine).Assembly });
        var result = pendingResult.Result;
        
        Assert.Equal(expectedOutput, result, ignoreLineEndingDifferences: true);
    }
}
