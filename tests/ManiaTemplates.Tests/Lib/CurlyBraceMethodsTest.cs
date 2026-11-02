using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Tests.Lib;

public class CurlyBraceMethodsTest
{
    [Theory]
    [InlineData("{{ {{ a }} {{ b }} }}")]
    [InlineData("{{ {{ b }} }}")]
    [InlineData("text {{ a }} {{ b {{ c }} }}")]
    [InlineData("{{ a {{ b")]
    public void Should_Throw_Interpolation_Recursion_Exception(string value)
    {
        Assert.Throws<InterpolationRecursionException>(() => ICurlyBraceMethods.PreventInterpolationRecursion(value));
    }

    [Theory]
    [InlineData("{{ a }} {{ b }}")]
    [InlineData("{{a}}{{b}}")]
    [InlineData("{{ new[] { 1, 2 } }}")]
    [InlineData("no interpolation")]
    [InlineData("{{ a } }")]
    public void Should_Allow_Consecutive_Interpolations(string value)
    {
        ICurlyBraceMethods.PreventInterpolationRecursion(value);
    }

    [Fact]
    public void Should_Throw_Curly_Brace_Count_Mismatch_Exception()
    {
        Assert.Throws<CurlyBraceCountMismatchException>(() => ICurlyBraceMethods.PreventCurlyBraceCountMismatch("{{ { }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => ICurlyBraceMethods.PreventCurlyBraceCountMismatch("{{ } }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => ICurlyBraceMethods.PreventCurlyBraceCountMismatch("{"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => ICurlyBraceMethods.PreventCurlyBraceCountMismatch("}}"));
    }

    [Fact]
    public void Should_Replace_Curly_Braces()
    {
        Assert.Equal("abcd", ICurlyBraceMethods.ReplaceCurlyBraces("{{a}}{{ b }}{{c }}{{  d}}", s => s));
        Assert.Equal("x y z", ICurlyBraceMethods.ReplaceCurlyBraces("{{x}} {{ y }} {{z }}", s => s));
        Assert.Equal("unittest", ICurlyBraceMethods.ReplaceCurlyBraces("{{ unit }}test", s => s));
        Assert.Equal("#unit#test", ICurlyBraceMethods.ReplaceCurlyBraces("{{ unit }}test", s => $"#{s}#"));
        Assert.Equal("#{ unit#}test", ICurlyBraceMethods.ReplaceCurlyBraces("{{{ unit }}}test", s => $"#{s}#"));
    }
}