using ManiaTemplates.Exceptions;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class CurlyBraceUtilsTest
{
    [Theory]
    [InlineData("{{ {{ a }} {{ b }} }}")]
    [InlineData("{{ {{ b }} }}")]
    [InlineData("text {{ a }} {{ b {{ c }} }}")]
    [InlineData("{{ a {{ b")]
    public void Should_Throw_Interpolation_Recursion_Exception(string value)
    {
        Assert.Throws<InterpolationRecursionException>(() => CurlyBraceUtils.PreventInterpolationRecursion(value));
    }

    [Theory]
    [InlineData("{{ a }} {{ b }}")]
    [InlineData("{{a}}{{b}}")]
    [InlineData("{{ new[] { 1, 2 } }}")]
    [InlineData("no interpolation")]
    public void Should_Allow_Non_Nested_Interpolations(string value)
    {
        CurlyBraceUtils.PreventInterpolationRecursion(value);
    }

    [Fact]
    public void Should_Throw_Curly_Brace_Count_Mismatch_Exception()
    {
        Assert.Throws<CurlyBraceCountMismatchException>(() => CurlyBraceUtils.PreventCurlyBraceCountMismatch("{{ { }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => CurlyBraceUtils.PreventCurlyBraceCountMismatch("{{ } }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => CurlyBraceUtils.PreventCurlyBraceCountMismatch("{"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => CurlyBraceUtils.PreventCurlyBraceCountMismatch("}}"));
    }

    [Fact]
    public void Should_Replace_Curly_Braces()
    {
        Assert.Equal("abcd", CurlyBraceUtils.ReplaceCurlyBraces("{{a}}{{ b }}{{c }}{{  d}}", s => s));
        Assert.Equal("x y z", CurlyBraceUtils.ReplaceCurlyBraces("{{x}} {{ y }} {{z }}", s => s));
        Assert.Equal("unittest", CurlyBraceUtils.ReplaceCurlyBraces("{{ unit }}test", s => s));
        Assert.Equal("#unit#test", CurlyBraceUtils.ReplaceCurlyBraces("{{ unit }}test", s => $"#{s}#"));
        Assert.Equal("#{ unit#}test", CurlyBraceUtils.ReplaceCurlyBraces("{{{ unit }}}test", s => $"#{s}#"));
    }
}