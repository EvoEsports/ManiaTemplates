using System.Reflection;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Xunit.Abstractions;

namespace ManiaTemplates.Tests.Lib;

public class MtTransformerTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();
    private readonly Regex _hashCodePattern = new("[0-9]{6,10}");

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
        TemplateContent = @"<Frame if=""enabled"" foreach=""int i in numbers"" x=""{{ 20 * __index }}"">
                                <Label if=""i &lt; numbers.Count"" foreach=""int j in numbers.GetRange(0, i)"" text=""{{ i }}, {{ j }} at index {{ __index }}, {{ __index2 }}"" />
                            </Frame>
                            <Frame>
                                <Frame>
                                    <test>
                                        <Graph arg3=""{{ new test() }}""/>
                                    </test>
                                </Frame>
                            </Frame>"
    };

    private readonly MtTransformer _transformer;

    public MtTransformerTest()
    {
        _transformer = new MtTransformer(_maniaTemplateEngine, new MtLanguageT4());
    }

    [Fact]
    public void Should_Build_Manialink()
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
        var generalizedResult = TransformCodeToOrderNumber(result);

        Assert.Equal(expected, generalizedResult, ignoreLineEndingDifferences: true);
    }

    private string TransformCodeToOrderNumber(string input)
    {
        var count = 0;
        var assignments = new Dictionary<string, string>();
        return _hashCodePattern.Replace(input, delegate(Match match)
        {
            if (assignments.TryGetValue(match.Value, out var value))
            {
                return value;
            }

            count++;
            assignments[match.Value] = count.ToString();
            return count.ToString();
        });
    }

    [Fact]
    public void Should_Import_Components_Recursively()
    {
        var assemblies = new List<Assembly>();
        assemblies.Add(Assembly.GetExecutingAssembly());
        
        _maniaTemplateEngine.AddTemplateFromString("Embeddable", "<component><template><el/></template></component>");
        _maniaTemplateEngine.AddTemplateFromString("RecursionElement", "<component><import component='Embeddable' as='Comp' /><template><Comp/></template></component>");
        _maniaTemplateEngine.AddTemplateFromString("RecursionRoot", "<component><import component='RecursionElement' as='REL' /><template><REL/></template></component>");
        
        var output = _maniaTemplateEngine.RenderAsync("RecursionRoot", new {}, assemblies).Result;
        Assert.Equal( @$"<manialink version=""3"" id=""MtRecursionRoot"" name=""EvoSC#-MtRecursionRoot"">
<el />
<script><!--
--></script>
</manialink>
", output, ignoreLineEndingDifferences: true);
    }
}