using System.Reflection;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class TransformerTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();
    private readonly Regex _renderMethodSuffixPattern = new("_[A-Z0-9]+\\(");

    private readonly MtComponent _testComponent = new()
    {
        Namespaces = new() { "namespace" },
        Properties =
            new()
            {
                { "numbers", new() { Name = "numbers", Type = "List<int>" } },
                { "enabled", new() { Name = "enabled", Type = "boolean", Default = "true" } }
            },
        Scripts = new()
        {
            new() { Content = "scriptText1", Main = true, Once = false },
            new() { Content = "scriptText2", Main = false, Once = true },
            new() { Content = "scriptText3", Main = false, Once = false }
        },
        HasSlot = true,
        ImportedComponents =
            new()
            {
                { "component", new() { TemplateKey = "component.mt", Tag = "component" } },
                { "Graph", new() { TemplateKey = "newGraph.mt", Tag = "Graph" } }
            },
        TemplateContent =
            @"<Label if=""enabled"" foreach=""var i in numbers"" x=""{{ 20 * __index }}"" text=""{{ i }} at index {{ __index }}"" />Text<!--Comment--><test><Graph/></test><slot />"
    };

    private readonly Transformer _transformer;

    public TransformerTest()
    {
        _transformer = new Transformer(_maniaTemplateEngine, new MtLanguageT4());
    }

    [Fact]
    public void ShouldBuildManialink()
    {
        var components = new Dictionary<string, MtComponent>
        {
            {
                "newGraph.mt",
                new MtComponent
                {
                    Namespaces = new() { "GraphNamespace" },
                    Properties =
                        new()
                        {
                            { "arg1", new() { Name = "arg1", Type = "string", Default = "" } },
                            { "arg2", new() { Name = "arg2", Type = "int", Default = "0" } },
                            { "arg3", new() { Name = "arg3", Type = "test" } }
                        },
                    Scripts = new List<MtComponentScript>()
                    {
                        new() { Content = "GraphScript", Main = false, Once = false },
                        new() { Content = "GraphScript", Main = false, Once = true }
                    },
                    HasSlot = false,
                    ImportedComponents = new MtComponentMap(),
                    TemplateContent = ""
                }
            }
        };
        _maniaTemplateEngine.GetType().GetField("_components", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_maniaTemplateEngine, components);

        var expected = File.ReadAllText("Lib/expected.tt");

        var result = _transformer.BuildManialink(_testComponent, "expected");

        Assert.Equal(_renderMethodSuffixPattern.Replace(result, "("), expected);
    }
}
