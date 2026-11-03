using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Xunit.Abstractions;

namespace ManiaTemplates.Tests.Lib;

public class MtTransformerTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly MtTransformer _transformer;
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
            new() { Content = "scriptText1", HasMainMethod = true, Once = false },
            new() { Content = "scriptText2", HasMainMethod = false, Once = true },
            new() { Content = "scriptText3", HasMainMethod = false, Once = false }
        },
        HasSlot = true,
        ImportedComponents =
            new()
            {
                { "component", new() { TemplateKey = "component.mt", Tag = "component" } },
                { "Graph", new() { TemplateKey = "newGraph.mt", Tag = "Graph" } }
            },
        TemplateContent = @"<frame if=""enabled"">
                                <Frame foreach=""int i in numbers"" x=""{{ 20 * __index }}"">
                                    <frame if=""i &lt; numbers.Count"">
                                        <Label foreach=""int j in numbers.GetRange(0, i)"" text=""{{ i }}, {{ j }} at index {{ __index }}, {{ __index2 }}"" />
                                    </frame>
                                </Frame>
                            </frame>
                            <Frame>
                                <Frame>
                                    <test>
                                        <Graph arg3=""{{ new test() }}""/>
                                    </test>
                                </Frame>
                            </Frame>"
    };

    public MtTransformerTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _transformer = new MtTransformer(_maniaTemplateEngine, new MtLanguageT4());
    }

    [Fact]
    public void Should_Build_Manialink()
    {
        var components = new ConcurrentDictionary<string, MtComponent>
        {
            ["newGraph.mt"] = new MtComponent
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
                    new() { Content = "GraphScript", HasMainMethod = false, Once = false },
                    new() { Content = "GraphScript", HasMainMethod = false, Once = true },
                },
                HasSlot = false,
                ImportedComponents = new MtComponentMap(),
                TemplateContent = ""
            }
        };

        _maniaTemplateEngine.GetType().GetField("_components", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_maniaTemplateEngine, components);

        var expected = File.ReadAllText("Lib/expected.tt");
        var result = _transformer.BuildManialink(_testComponent, "expected");
        var generalizedResult = TransformCodeToOrderNumber(result);

        _testOutputHelper.WriteLine(generalizedResult);

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
        var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };

        _maniaTemplateEngine.AddTemplateFromString("Embeddable", "<component><template><el/></template></component>");
        _maniaTemplateEngine.AddTemplateFromString("RecursionElement",
            "<component><import component='Embeddable' as='Comp' /><template><Comp/></template></component>");
        _maniaTemplateEngine.AddTemplateFromString("RecursionRoot",
            "<component><import component='RecursionElement' as='REL' /><template><REL/></template></component>");

        var output = _maniaTemplateEngine.RenderAsync("RecursionRoot", new { }, assemblies).Result;
        Assert.Equal(@$"<manialink version=""3"" id=""MtRecursionRoot"" name=""EvoSC#-MtRecursionRoot"">
<el />
</manialink>
", output, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Join_Feature_Blocks()
    {
        var language = new MtLanguageT4();
        
        Assert.Equal("<#+\n unittest \n#>",
            language.OptimizeOutput("<#+#><#+\n #>\n<#+ unittest \n#><#+ \n\n\n#>"));
    }

    [Fact]
    public void Should_Detect_Correct_Type_String_For_CSharp_Scripting()
    {
        Assert.Equal("int", MtTransformer.GetFormattedTypeName(0.GetType()));
        Assert.Equal("double", MtTransformer.GetFormattedTypeName(0.0.GetType()));
        Assert.Equal("string", MtTransformer.GetFormattedTypeName("test".GetType()));
        Assert.Equal("System.Collections.Generic.List<string>",
            MtTransformer.GetFormattedTypeName(new List<string>().GetType()));
        Assert.Equal("System.Collections.Generic.HashSet<string>",
            MtTransformer.GetFormattedTypeName(new HashSet<string>().GetType()));
        Assert.Equal("dynamic", MtTransformer.GetFormattedTypeName(new TestDynamicObject().GetType()));
    }
}