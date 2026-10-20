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

    [Theory]
    [InlineData("if", "a > 1", MtConditionType.If)]
    [InlineData("else-if", "a > 1", MtConditionType.ElseIf)]
    [InlineData("else", "", MtConditionType.Else)]
    public void Should_Pull_Condition(string attributeName, string expression, MtConditionType type)
    {
        _mtComponentAttributes[attributeName] = new MtComponentAttribute { Value = expression };
        var condition = _mtComponentAttributes.PullCondition();

        Assert.NotNull(condition);
        Assert.Equal(type, condition.Type);
        Assert.Equal(expression, condition.Expression);
        Assert.Equal(attributeName, condition.AttributeName);
        Assert.Empty(_mtComponentAttributes);
    }

    [Fact]
    public void Should_Return_Null_Without_Condition()
    {
        _mtComponentAttributes["text"] = new MtComponentAttribute { Value = "value" };
        Assert.Null(_mtComponentAttributes.PullCondition());
        Assert.Single(_mtComponentAttributes);
    }

    [Theory]
    [InlineData("if")]
    [InlineData("else-if")]
    public void Should_Reject_Empty_Condition(string attributeName)
    {
        _mtComponentAttributes[attributeName] = new MtComponentAttribute { Value = " " };
        Assert.Throws<EmptyNodeAttributeException>(() => _mtComponentAttributes.PullCondition());
    }
}