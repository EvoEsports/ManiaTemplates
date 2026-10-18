using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.ControlElements;

public class MtForeachTest
{
    [Theory]
    [InlineData("int i in numbers", "int i")]
    [InlineData("int[] row in matrix", "int[] row")]
    [InlineData("KeyValuePair<string, int> pair in dict", "KeyValuePair<string, int> pair")]
    [InlineData("ref readonly int x in span", "int x")]
    [InlineData("string name in names.Where(n => n.Contains(\" in \"))", "string name")]
    [InlineData("(int a, string b) in pairs", "int a", "string b")]
    [InlineData("(int a, string b, bool c) in triples", "int a", "string b", "bool c")]
    [InlineData("(int a, (List<int> b, bool c)) in nested", "int a", "List<int> b", "bool c")]
    [InlineData("(int a, _) in pairs", "int a")]
    [InlineData("(int a, int _) in pairs", "int a")]
    public void Should_Parse_Loop_Variables(string foreachAttributeValue, params string[] expectedVariables)
    {
        var loop = MtForeach.FromString(foreachAttributeValue, new MtDataContext(), 1, 1);

        Assert.Equal("foreach", loop.Keyword);
        Assert.Equal(foreachAttributeValue, loop.Condition);
        Assert.Equal(expectedVariables, loop.Variables.Select(variable => $"{variable.Type} {variable.Name}"));
        Assert.All(loop.Variables, variable => Assert.Equal(variable.Type, loop.Context[variable.Name]));
        Assert.Equal("int", loop.Context["__index"]);
    }

    [Fact]
    public void Should_Name_Index_Variable_By_Depth()
    {
        var outer = MtForeach.FromString("int i in numbers", new MtDataContext(), 1, 1);
        var inner = MtForeach.FromString("int j in numbers", outer.Context, 1, 2);

        Assert.Equal("__index", outer.IndexVariableName);
        Assert.Equal("__index2", inner.IndexVariableName);
        Assert.Equal(["__index", "__index2", "i", "j"], inner.Context.Keys.Order());
    }

    [Theory]
    [InlineData("var i in numbers")]
    [InlineData("var (a, b) in pairs")]
    [InlineData("(int a, var b) in pairs")]
    [InlineData("int __i in numbers")]
    [InlineData("(int a, string __b) in pairs")]
    [InlineData("int i in")]
    [InlineData("int i = 0; i < 3; i++")]
    [InlineData("(x.a, int b) in pairs")]
    [InlineData("int i in numbers) {} foreach (int j in numbers")]
    public void Should_Reject_Invalid_Loops(string foreachAttributeValue)
    {
        Assert.Throws<ParsingForeachLoopFailedException>(() =>
            MtForeach.FromString(foreachAttributeValue, new MtDataContext(), 1, 1));
    }
}
