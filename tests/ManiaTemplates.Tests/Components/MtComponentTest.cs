using System.Reflection;
using FluentAssertions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.Components;

public class MtComponentTest
{
    private readonly ManiaTemplateEngine _engine = new();

    [Fact]
    public void ShouldReadEmptyTemplate()
    {
        const string Component = "<component/>";
        var expected = new MtComponent
        {
            Namespaces = new(),
            Properties = new(),
            Scripts = new(),
            HasSlot = false,
            ImportedComponents = new(),
            TemplateContent = ""
        };

        var result = MtComponent.FromTemplate(_engine, Component);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldPopulatedTemplate()
    {
        const string Component = """
            <component>
                <property type="int" name="number" default="0"/>
                <property type="string" name="text"/>
                
                <import component="name1.mt"/>
                <import component="name2.mt" as="alias"/>
                
                <using namespace="namespace"/>
                
                <template>
                    <node attr="value"/>
                    <slot/>
                </template>
                
                <script resource="res" main="notUsed">scriptText1</script>
                <script once="notUsed">scriptText1</script>
                <script once="notUsed">scriptText2</script>
                <script>scriptText3</script>
            </component>
        """;
        _engine.GetType().GetField("_maniaScripts", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_engine,
            new Dictionary<string, string> { { "res", "resourceScript" } });
        var expected = new MtComponent
        {
            Namespaces = new() { "namespace" },
            Properties =
                new()
                {
                    { "number", new() { Name = "number", Type = "int", Default = "0" } },
                    { "text", new() { Name = "text", Type = "string" } }
                },
            Scripts = new()
            {
                new() { Content = "resourceScript", Main = true, Once = false },
                new() { Content = "scriptText1", Main = false, Once = true },
                new() { Content = "scriptText2", Main = false, Once = true },
                new() { Content = "scriptText3", Main = false, Once = false }
            },
            HasSlot = true,
            ImportedComponents = new()
            {
                { "name1", new() { TemplateKey = "name1.mt", Tag = "name1" } },
                { "alias", new() { TemplateKey = "name2.mt", Tag = "alias" } }
            },
            TemplateContent = @"<node attr=""value"" /><slot />"
        };

        var result = MtComponent.FromTemplate(_engine, Component);

        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(@"<property name=""text""/>", "property", "type")]
    [InlineData(@"<property type=""int""/>", "property", "name")]
    [InlineData(@"<import/>", "import", "component")]
    [InlineData(@"<using/>", "using", "namespace")]
    public void ShouldNotReadTemplateMissingAttributes(string content, string nodeName, string attributeName)
    {
        var component = $"<component>{content}</component>";

        var exception = Assert.Throws<Exception>(() => MtComponent.FromTemplate(_engine, component));
        Assert.Contains(nodeName, exception.Message);
        Assert.Contains($"'{attributeName}'", exception.Message);
    }
}
