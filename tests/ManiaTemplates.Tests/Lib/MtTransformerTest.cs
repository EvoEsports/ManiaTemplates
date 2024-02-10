using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
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

    // [Fact]
    // public void Should_Throw_Interpolation_Recursion_Exception()
    // {
    //     Assert.Throws<InterpolationRecursionException>(() =>
    //         MtTransformer.CheckInterpolationRecursion("{{ {{ a }} {{ b }} }}"));
    //     Assert.Throws<InterpolationRecursionException>(() =>
    //         MtTransformer.CheckInterpolationRecursion("{{ {{ b }} }}"));
    // }

    [Fact]
    public void Should_Throw_Curly_Brace_Count_Mismatch_Exception()
    {
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{{ { }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{{ } }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("}}"));
    }

    [Fact]
    public void Should_Replace_Curly_Braces()
    {
        Assert.Equal("abcd", _transformer.ReplaceCurlyBraces("{{a}}{{ b }}{{c }}{{  d}}", s => s));
        Assert.Equal("x y z", _transformer.ReplaceCurlyBraces("{{x}} {{ y }} {{z }}", s => s));
        Assert.Equal("unittest", _transformer.ReplaceCurlyBraces("{{ unit }}test", s => s));
        Assert.Equal("#unit#test", _transformer.ReplaceCurlyBraces("{{ unit }}test", s => $"#{s}#"));
        Assert.Equal("#{ unit#}test", _transformer.ReplaceCurlyBraces("{{{ unit }}}test", s => $"#{s}#"));
    }

    [Fact]
    public void Should_Wrap_Strings_In_Quotes()
    {
        Assert.Equal(@"$""unit test""", MtTransformer.WrapStringInQuotes("unit test"));
        Assert.Equal(@"$""""", MtTransformer.WrapStringInQuotes(""));
    }

    [Fact]
    public void Should_Join_Feature_Blocks()
    {
        Assert.Equal("<#+\n unittest \n#>",
            MtTransformer.JoinFeatureBlocks("<#+#><#+\n #> <#+ unittest \n#><#+ \n\n\n#>"));
    }

    [Fact]
    public void Should_Convert_String_To_Xml_Node()
    {
        var node = MtTransformer.XmlStringToNode("<unit>test</unit>");
        Assert.IsAssignableFrom<XmlNode>(node);
        Assert.Equal("test", node.InnerText);
        Assert.Equal("<unit>test</unit>", node.InnerXml);
        Assert.Equal("<doc><unit>test</unit></doc>", node.OuterXml);
    }

    [Fact]
    public void Should_Create_Xml_Opening_Tag()
    {
        var attributeList = new MtComponentAttributes();
        Assert.Equal("<test />", _transformer.CreateXmlOpeningTag("test", attributeList, false));
        Assert.Equal("<test>", _transformer.CreateXmlOpeningTag("test", attributeList, true));

        attributeList["prop"] = "value";
        Assert.Equal("""<test prop="value" />""", _transformer.CreateXmlOpeningTag("test", attributeList, false));
        Assert.Equal("""<test prop="value">""", _transformer.CreateXmlOpeningTag("test", attributeList, true));
    }

    [Fact]
    public void Should_Create_ManiaLink_Opening_Tag()
    {
        Assert.Equal("""<manialink version="99" id="Test" name="EvoSC#-Test">""",
            MtTransformer.ManiaLinkStart("Test", 99));
        Assert.Equal("""<manialink version="99" id="Test" name="EvoSC#-Test" layer="SomeLayer">""",
            MtTransformer.ManiaLinkStart("Test", 99, "SomeLayer"));
    }

    [Fact]
    public void Should_Convert_Xml_Node_Arguments_To_MtComponentAttributes_Instance()
    {
        var node = MtTransformer.XmlStringToNode("""<testNode arg1="test1" arg2="test2">testContent</testNode>""");
        if (node.FirstChild == null) return;

        var attributes = MtTransformer.GetXmlNodeAttributes(node.FirstChild);
        Assert.Equal(2, attributes.Count);
        Assert.Equal("test1", attributes["arg1"]);
        Assert.Equal("test2", attributes["arg2"]);
    }

    [Fact]
    public void Should_Detect_Correct_Type_String_For_CSharp_Scripting()
    {
        Assert.Equal("int", MtTransformer.GetFormattedName(0.GetType()));
        Assert.Equal("double", MtTransformer.GetFormattedName(0.0.GetType()));
        Assert.Equal("string", MtTransformer.GetFormattedName("test".GetType()));
        Assert.Equal("System.Collections.Generic.List<string>",
            MtTransformer.GetFormattedName(new List<string>().GetType()));
        Assert.Equal("System.Collections.Generic.HashSet<string>",
            MtTransformer.GetFormattedName(new HashSet<string>().GetType()));
        Assert.Equal("dynamic", MtTransformer.GetFormattedName(new TestDynamicObject().GetType()));
    }
}