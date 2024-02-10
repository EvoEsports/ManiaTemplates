using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Tests.Lib;

public class CurlyBraceMethodsTest
{
    // [Fact]
    // public void Should_Throw_Interpolation_Recursion_Exception()
    // {
    //     Assert.Throws<InterpolationRecursionException>(() => _transformer.PreventInterpolationRecursion("{{ {{ a }} {{ b }} }}"));
    //     Assert.Throws<InterpolationRecursionException>(() => _transformer.PreventInterpolationRecursion("{{ {{ b }} }}"));
    // }

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