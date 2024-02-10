using ManiaTemplates.Exceptions;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Xunit.Abstractions;

namespace ManiaTemplates.Tests.Lib;

public class CurlyBraceMethodsTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly MtTransformer _transformer;
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();

    public CurlyBraceMethodsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _transformer = new MtTransformer(_maniaTemplateEngine, new MtLanguageT4());
    }
    
    // [Fact]
    // public void Should_Throw_Interpolation_Recursion_Exception()
    // {
    //     Assert.Throws<InterpolationRecursionException>(() => _transformer.PreventInterpolationRecursion("{{ {{ a }} {{ b }} }}"));
    //     Assert.Throws<InterpolationRecursionException>(() => _transformer.PreventInterpolationRecursion("{{ {{ b }} }}"));
    // }

    [Fact]
    public void Should_Throw_Curly_Brace_Count_Mismatch_Exception()
    {
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{{ { }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{{ } }}"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("{"));
        Assert.Throws<CurlyBraceCountMismatchException>(() => _transformer.PreventCurlyBraceCountMismatch("}}"));
    }

    [Fact]
    public void Should_Replace_Curly_Braces()
    {
        Assert.Equal("abcd", _transformer.ReplaceCurlyBraces("{{a}}{{ b }}{{c }}{{  d}}", s => s));
        Assert.Equal("x y z", _transformer.ReplaceCurlyBraces("{{x}} {{ y }} {{z }}", s => s));
        Assert.Equal("unittest", _transformer.ReplaceCurlyBraces("{{ unit }}test", s => s));
        Assert.Equal("#unit#test", _transformer.ReplaceCurlyBraces("{{ unit }}test", s => $"#{s}#"));
        Assert.Equal("#{ unit#}test", _transformer.ReplaceCurlyBraces("{{{ unit }}}test", s => $"#{s}#"));
    }
}