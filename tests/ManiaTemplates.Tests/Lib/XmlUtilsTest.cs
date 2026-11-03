using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class XmlUtilsTest
{
    [Fact]
    public void Should_Convert_String_To_Xml_Node()
    {
        var node = XmlUtils.NodeFromString("<unit>test</unit>");
        Assert.Equal("test", node.InnerText);
        Assert.Equal("<unit>test</unit>", node.InnerXml);
        Assert.Equal("<doc><unit>test</unit></doc>", node.OuterXml);
    }
    [Fact]
    public void Should_Create_Xml_Opening_Tag()
    {
        var attributeList = new MtComponentAttributes();
        var lang = new MtLanguageT4();
        Assert.Equal("<test />", XmlUtils.CreateOpeningTag("test", attributeList, false, lang.InsertResult));
        Assert.Equal("<test>", XmlUtils.CreateOpeningTag("test", attributeList, true, lang.InsertResult));

        attributeList["prop"] = new MtComponentAttribute{Value = "value"};
        Assert.Equal("""<test prop="value" />""", XmlUtils.CreateOpeningTag("test", attributeList, false, lang.InsertResult));
        Assert.Equal("""<test prop="value">""", XmlUtils.CreateOpeningTag("test", attributeList, true, lang.InsertResult));
    }
    [Fact]
    public void Should_Convert_Xml_Node_Arguments_To_MtComponentAttributes_Instance()
    {
        var node = XmlUtils.NodeFromString("""<testNode arg1="test1" arg2="test2">testContent</testNode>""");
        Assert.NotNull(node.FirstChild);

        var attributes = XmlUtils.GetAttributes(node.FirstChild);
        Assert.Equal(2, attributes.Count);
        Assert.Equal("test1", attributes["arg1"].Value);
        Assert.Equal("test2", attributes["arg2"].Value);
    }
}
