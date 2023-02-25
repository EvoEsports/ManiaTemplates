using System.Text.RegularExpressions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public partial class MtTemplateFile : IMtTemplate
{
    public required string Filename { get; init; }

    public async Task<string> GetContent() => await File.ReadAllTextAsync(Filename);

    public string GetXmlTag() => ClassNameMatcher().Replace(Path.GetFileName(Filename), "$1");

    public string? GetBasePath() => Path.GetDirectoryName(Filename);

    [GeneratedRegex("^(.+)\\.\\w+$")]
    private static partial Regex ClassNameMatcher();
}