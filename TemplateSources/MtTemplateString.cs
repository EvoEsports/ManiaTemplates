using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public class MtTemplateString : IMtTemplate
{
    public required string Content { get; init; }

    public Task<string> GetContent() => Task.FromResult(Content);
}