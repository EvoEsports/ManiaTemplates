using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Interfaces;
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

    public MtTransformerTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
                        new() { Content = "GraphScript", HasMainMethod = false, Once = false },
                        new() { Content = "GraphScript", HasMainMethod = false, Once = true },
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
    public void Should_Wrap_Strings_In_Quotes()
    {
        Assert.Equal(@"$""unit test""", IStringMethods.WrapStringInQuotes("unit test"));
        Assert.Equal(@"$""""", IStringMethods.WrapStringInQuotes(""));
    }

    [Fact]
    public void Should_Join_Feature_Blocks()
    {
        var language = new MtLanguageT4();
        
        Assert.Equal("<#+\n unittest \n#>",
            language.OptimizeOutput("<#+#><#+\n #>\n<#+ unittest \n#><#+ \n\n\n#>"));
    }

    [Fact]
    public void Should_Convert_String_To_Xml_Node()
    {
        var node = IXmlMethods.NodeFromString("<unit>test</unit>");
        Assert.IsAssignableFrom<XmlNode>(node);
        Assert.Equal("test", node.InnerText);
        Assert.Equal("<unit>test</unit>", node.InnerXml);
        Assert.Equal("<doc><unit>test</unit></doc>", node.OuterXml);
    }

    [Fact]
    public void Should_Create_Xml_Opening_Tag()
    {
        var attributeList = new MtComponentAttributes();
        var lang = new MtLanguageT4();
        Assert.Equal("<test />", IXmlMethods.CreateOpeningTag("test", attributeList, false, lang.InsertResult));
        Assert.Equal("<test>", IXmlMethods.CreateOpeningTag("test", attributeList, true, lang.InsertResult));

        attributeList["prop"] = new MtComponentAttribute{Value = "value"};
        Assert.Equal("""<test prop="value" />""", IXmlMethods.CreateOpeningTag("test", attributeList, false, lang.InsertResult));
        Assert.Equal("""<test prop="value">""", IXmlMethods.CreateOpeningTag("test", attributeList, true, lang.InsertResult));
    }

    [Fact]
    public void Should_Create_ManiaLink_Opening_Tag()
    {
        Assert.Equal("""<manialink version="99" id="Test" name="EvoSC#-Test">""",
            ManiaLink.OpenTag("Test", 99));
        Assert.Equal("""<manialink version="99" id="Test" name="EvoSC#-Test" layer="SomeLayer">""",
            ManiaLink.OpenTag("Test", 99, "SomeLayer"));
    }

    [Fact]
    public void Should_Convert_Xml_Node_Arguments_To_MtComponentAttributes_Instance()
    {
        var node = IXmlMethods.NodeFromString("""<testNode arg1="test1" arg2="test2">testContent</testNode>""");
        if (node.FirstChild == null) return;

        var attributes = IXmlMethods.GetAttributes(node.FirstChild);
        Assert.Equal(2, attributes.Count);
        Assert.Equal("test1", attributes["arg1"].Value);
        Assert.Equal("test2", attributes["arg2"].Value);
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