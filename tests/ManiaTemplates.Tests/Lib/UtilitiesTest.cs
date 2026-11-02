using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class UtilitiesTest
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData("<frame><label/></frame>", "<frame>\n  <label />\n</frame>")]
    public void Should_Format_Xml(string? input, string expected)
    {
        Assert.Equal(expected, Helper.PrettyXml(input), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task Should_Load_Embedded_Resource()
    {
        var content = await Helper.GetEmbeddedResourceContentAsync("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
            typeof(ManiaTemplateEngine).Assembly);

        Assert.Contains("main()", content);
    }

    [Fact]
    public async Task Should_Throw_Exception_For_Missing_Embedded_Resource()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Helper.GetEmbeddedResourceContentAsync("Missing.Resource", typeof(ManiaTemplateEngine).Assembly));
    }

    [Theory]
    [InlineData("test", "Mttest", "EvoSC#-Mttest")]
    [InlineData("Folder.My-Template.mt", "MtFolderMyTemplate", "EvoSC#-MtFolderMyTemplate")]
    [InlineData("Some.Window.xml", "MtSomeWindow", "EvoSC#-MtSomeWindow")]
    public void Should_Convert_Template_Keys_To_Manialink_Identifiers(string key, string expectedId,
        string expectedName)
    {
        Assert.Equal(expectedId, ManialinkNameUtils.KeyToId(key));
        Assert.Equal(expectedName, ManialinkNameUtils.KeyToName(key));
    }

    [Theory]
    [InlineData("string", "name", null, "string name", null)]
    [InlineData("string", "name", "unit", "string name = $\"unit\"", "$\"unit\"")]
    [InlineData("int", "count", "3", "int count = 3", "3")]
    public void Should_Convert_Component_Properties_To_Code(string type, string name, string? defaultValue,
        string expectedArgument, string? expectedDefault)
    {
        var property = new MtComponentProperty { Type = type, Name = name, Default = defaultValue };

        Assert.Equal(expectedDefault, property.GetDefaultWrapped());
        Assert.Equal(expectedArgument, property.ToMethodArgument());
        Assert.Equal($"const {expectedArgument};", property.ToLocalConstant());
    }

    [Fact]
    public void Should_Append_Snippets()
    {
        var content = new Snippet().AppendLine("a").AppendLine("b");

        var snippet = new Snippet()
            .AppendSnippet(content)
            .AppendSnippet(1, content)
            .AppendSnippet(null, content);

        Assert.Equal(new[] { "a", "b", "a", "b", "a", "b" }, snippet);
        Assert.Equal("a,b,a,b,a,b", snippet.ToString(","));
    }

    [Fact]
    public void Should_Create_T4_Output_Statements()
    {
        var language = new MtLanguageT4();

        Assert.Equal("<#= (value) #>", language.InsertResult("value"));
        Assert.Equal("<#= Security.Escape(value) #>", language.InsertResultEscaped("value"));
    }

    [Fact]
    public void Should_Create_Component_Node()
    {
        var component = new MtComponent
        {
            TemplateContent = "<label/>",
            HasSlot = true,
            ImportedComponents = new MtComponentMap(),
            Properties = new(),
            Namespaces = new(),
            Scripts = new()
        };
        var attributes = new MtComponentAttributes();
        var context = new MtDataContext("context");

        var node = new MtComponentNode("Comp", component, attributes, "<Comp/>", context);

        Assert.Equal("Comp", node.Tag);
        Assert.Same(component, node.MtComponent);
        Assert.Same(attributes, node.Attributes);
        Assert.Equal("<Comp/>", node.TemplateContent);
        Assert.Same(context, node.Context);
        Assert.True(node.HasSlot);
    }
}
