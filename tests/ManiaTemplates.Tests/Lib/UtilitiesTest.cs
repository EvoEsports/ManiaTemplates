using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class UtilitiesTest
{
    [Fact]
    public void Should_Throw_Exception_For_Missing_Embedded_Resource()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EmbeddedResourceUtils.GetEmbeddedResourceContent("Missing.Resource", typeof(ManiaTemplateEngine).Assembly));
    }

    [Theory]
    [InlineData("test", "Mttest")]
    [InlineData("Folder.My-Template.mt", "MtFolderMyTemplate")]
    [InlineData("Some.Window.xml", "MtSomeWindow")]
    public void Should_Convert_Template_Keys_To_Manialink_Identifiers(string key, string expectedId)
    {
        Assert.Equal(expectedId, ManialinkNameUtils.KeyToId(key));
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
}
