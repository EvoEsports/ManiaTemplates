using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public class MtTemplateString : IMtTemplate
{
    public required string TemplateString { get; init; }
    public required string XmlTag { get; init; }
    public required string WorkingDirectory { get; init; }

    public Task<string> GetContent() => Task.FromResult(TemplateString);

    public string GetXmlTag() => XmlTag;

    public string GetBasePath() => WorkingDirectory;
}