using System.Reflection;
using FluentAssertions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.Components;

public class MtComponentTest
{
    private readonly ManiaTemplateEngine _engine = new();

    [Fact]
    public void Should_Read_Empty_Template()
    {
        const string component = "<component/>";
        
        var expected = new MtComponent
        {
            Namespaces = new(),
            Properties = new(),
            Scripts = new(),
            HasSlot = false,
            ImportedComponents = new(),
            TemplateContent = ""
        };

        var result = MtComponent.FromTemplate(_engine, component);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Should_Populate_Template()
    {
        const string component = """
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
                
                <script resource="res"><!--main(){}--></script>
                <script once="notUsed">scriptText1</script>
                <script once="notUsed">scriptText2</script>
                <script>scriptText3</script>
            </component>
        """;
        
        _engine.GetType().GetField("_maniaScripts", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_engine,
            new Dictionary<string, string> { { "res", "<!--main(){}-->" } });
        
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
                new() { Content = "<!--main(){}-->", HasMainMethod = true, Once = false },
                new() { Content = "scriptText1", HasMainMethod = false, Once = true },
                new() { Content = "scriptText2", HasMainMethod = false, Once = true },
                new() { Content = "scriptText3", HasMainMethod = false, Once = false }
            },
            HasSlot = true,
            ImportedComponents = new()
            {
                { "name1", new() { TemplateKey = "name1.mt", Tag = "name1" } },
                { "alias", new() { TemplateKey = "name2.mt", Tag = "alias" } }
            },
            TemplateContent = @"<node attr=""value"" /><slot />"
        };

        var result = MtComponent.FromTemplate(_engine, component);

        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(@"<property name=""text""/>")]
    [InlineData(@"<property type=""int""/>")]
    [InlineData(@"<import/>")]
    [InlineData(@"<using/>")]
    public void Should_Not_Read_Invalid_Nodes(string content)
    {
        var component = $"<component>{content}</component>";

        Assert.Throws<MissingAttributeException>(() => MtComponent.FromTemplate(_engine, component));
    }
}
