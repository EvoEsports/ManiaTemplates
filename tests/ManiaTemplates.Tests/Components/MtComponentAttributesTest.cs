using ManiaTemplates.Components;

namespace ManiaTemplates.Tests.Components;

public class MtComponentAttributesTest
{
    private readonly MtComponentAttributes _mtComponentAttributes = new();

    [Fact]
    public void Should_Contain_Single_Attribute_Pair()
    {
        _mtComponentAttributes["key"] = "value";
        Assert.Single(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Remove_Existing()
    {
        _mtComponentAttributes["key"] = "value";
        var result = _mtComponentAttributes.Pull("key");

        Assert.Equal("value", result);
        Assert.Empty(_mtComponentAttributes);
    }
}