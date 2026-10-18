using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.Components;

public class MtComponentAttributesTest
{
    private readonly MtComponentAttributes _mtComponentAttributes = new();

    [Fact]
    public void Should_Contain_Single_Attribute_Pair()
    {
        _mtComponentAttributes["key"] = new MtComponentAttribute{Value = "value"};
        Assert.Single(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Remove_Existing()
    {
        _mtComponentAttributes["key"] = new MtComponentAttribute{Value = "value"};
        var result = _mtComponentAttributes.Pull("key");

        Assert.Equal("value", result);
        Assert.Empty(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Pull_For_Loop()
    {
        _mtComponentAttributes["for"] = new MtComponentAttribute { Value = "int i = 0; i < 3; i++" };
        var loop = _mtComponentAttributes.PullLoop(new MtDataContext(), 1, 1);

        Assert.IsType<MtFor>(loop);
        Assert.Empty(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Pull_Foreach_Loop()
    {
        _mtComponentAttributes["foreach"] = new MtComponentAttribute { Value = "int i in numbers" };
        var loop = _mtComponentAttributes.PullLoop(new MtDataContext(), 1, 1);

        Assert.IsType<MtForeach>(loop);
        Assert.Empty(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Reject_For_And_Foreach_On_Same_Node()
    {
        _mtComponentAttributes["for"] = new MtComponentAttribute { Value = "int i = 0; i < 3; i++" };
        _mtComponentAttributes["foreach"] = new MtComponentAttribute { Value = "int j in numbers" };

        Assert.Throws<ParsingForLoopFailedException>(() =>
            _mtComponentAttributes.PullLoop(new MtDataContext(), 1, 1));
    }
}