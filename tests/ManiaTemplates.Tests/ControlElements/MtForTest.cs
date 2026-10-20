using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.ControlElements;

public class MtForTest
{
    [Fact]
    public void Should_Parse_Loop_Variable()
    {
        var loop = MtFor.FromString("int i = 0; i < 3; i++", new MtDataContext(), 1, 1);

        Assert.Equal("for", loop.Keyword);
        Assert.Equal("int i = 0; i < 3; i++", loop.Condition);
        var variable = Assert.Single(loop.Variables);
        Assert.Equal("int", variable.Type);
        Assert.Equal("i", variable.Name);
        Assert.Equal("int", loop.Context["i"]);
        Assert.Equal("int", loop.Context["__index"]);
    }

    [Fact]
    public void Should_Parse_Multiple_Declarators_With_Generic_Type()
    {
        var loop = MtFor.FromString(
            "List<int> a = new List<int> { 1, 2 }, b = Fn(x, y); a.Count < b.Count; a.Add(0)",
            new MtDataContext(), 1, 1);

        Assert.Equal(["a", "b"], loop.Variables.Select(variable => variable.Name));
        Assert.All(loop.Variables, variable => Assert.Equal("List<int>", variable.Type));
    }

    [Fact]
    public void Should_Allow_Loop_Without_Declaration()
    {
        var loop = MtFor.FromString("; count < 3; count++", new MtDataContext { { "count", "int" } }, 1, 1);

        Assert.Empty(loop.Variables);
        Assert.Equal("int", loop.Context["count"]);
    }

    [Fact]
    public void Should_Name_Index_Variable_By_Depth()
    {
        var outer = MtFor.FromString("int i = 0; i < 3; i++", new MtDataContext(), 1, 1);
        var inner = MtFor.FromString("int j = 0; j < 3; j++", outer.Context, 1, 2);

        Assert.Equal("__index", outer.IndexVariableName);
        Assert.Equal("__index2", inner.IndexVariableName);
        Assert.NotEqual(outer.CounterVariableName, inner.CounterVariableName);
        Assert.Equal(["__index", "__index2", "i", "j"], inner.Context.Keys.Order());
    }

    [Theory]
    [InlineData("var i = 0; i < 3; i++")]
    [InlineData("int __i = 0; __i < 3; __i++")]
    [InlineData("int i = 0; i < 3")]
    [InlineData("int i = 0; i < 3; i++) {} for (;;")]
    [InlineData("int i in numbers")]
    public void Should_Reject_Invalid_Loops(string forAttributeValue)
    {
        Assert.Throws<ParsingForLoopFailedException>(() =>
            MtFor.FromString(forAttributeValue, new MtDataContext(), 1, 1));
    }

    [Fact]
    public void Should_Reject_Variable_Declared_By_Surrounding_Loop()
    {
        var outer = MtForeach.FromString("int i in numbers", new MtDataContext(), 1, 1);

        Assert.Throws<ParsingForLoopFailedException>(() =>
            MtFor.FromString("int j = 0, i = 0; j < 3; j++", outer.Context, 1, 2));
    }

    [Fact]
    public void Should_Reject_Variable_Declared_Twice()
    {
        Assert.Throws<ParsingForLoopFailedException>(() =>
            MtFor.FromString("int i = 0, i = 1; i < 3; i++", new MtDataContext(), 1, 1));
    }
}
