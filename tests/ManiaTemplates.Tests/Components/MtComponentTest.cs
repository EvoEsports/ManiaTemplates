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
            Slots = new HashSet<string>(),
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
        
        _engine.AddManiaScriptFromString("res", "<!--main(){}-->");
        
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
                new() { Content = "main(){}", HasMainMethod = true, Once = false },
                new() { Content = "scriptText1", HasMainMethod = false, Once = true },
                new() { Content = "scriptText2", HasMainMethod = false, Once = true },
                new() { Content = "scriptText3", HasMainMethod = false, Once = false }
            },
            HasSlot = true,
            Slots = new HashSet<string>(){"default"},
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

    [Theory]
    [InlineData("my-prop")]
    [InlineData("1prop")]
    [InlineData("@class")]
    [InlineData("class")]
    [InlineData("checked")]
    [InlineData("__internal")]
    [InlineData("")]
    public void Should_Reject_Invalid_Property_Names(string name)
    {
        var component = $"<component><property type=\"string\" name=\"{name}\"/></component>";

        Assert.Throws<InvalidPropertyNameException>(() => MtComponent.FromTemplate(_engine, component));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("var")]
    [InlineData("_private")]
    public void Should_Allow_Contextual_Keywords_As_Property_Names(string name)
    {
        var component = $"<component><property type=\"string\" name=\"{name}\"/></component>";

        Assert.Contains(name, MtComponent.FromTemplate(_engine, component).Properties.Keys);
    }
}
