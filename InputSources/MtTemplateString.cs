using ManiaTemplates.Interfaces;

namespace ManiaTemplates.InputSources;

public class MtTemplateString : IMtTemplate
{
    public required string TemplateString { get; init; }
    public required string XmlTag { get; init; }

    public async Task<string> GetContent()
    {
        return TemplateString;
    }

    public string GetXmlTag()
    {
        return XmlTag;
    }

    public string? GetSourceDirectory()
    {
        return null;
    }
}