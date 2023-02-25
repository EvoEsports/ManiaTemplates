using System.Text.RegularExpressions;
using ManiaTemplates.Interfaces;
using ManiaTemplates.Lib;

namespace ManiaTemplates.InputSources;

public partial class MtTemplateFile : IMtTemplate
{
    public required string Filename { get; init; }
    public required string SourceDirectory { get; init; }
    
    public async Task<string> GetContent()
    {
        return await File.ReadAllTextAsync(Filename);
    }

    public string GetXmlTag()
    {
        return ClassNameMatcher().Replace(Path.GetFileName(Filename), "$1");
    }

    public string? GetSourceDirectory()
    {
        return SourceDirectory;
    }

    [GeneratedRegex("^(.+)\\.\\w+$")]
    private static partial Regex ClassNameMatcher();
}