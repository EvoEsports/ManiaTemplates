using System.Text.RegularExpressions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public partial class MtTemplateFile : IMtTemplate
{
    public required string Filename { get; init; }

    public async Task<string> GetContent() => await File.ReadAllTextAsync(Filename);
} 