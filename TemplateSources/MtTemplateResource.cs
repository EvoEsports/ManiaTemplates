using System.Reflection;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public class MtTemplateResource : IMtTemplate
{
    public required string ResourcePath { get; init; }
    public required Assembly SourceAssembly { get; init; }

    public async Task<string> GetContent()
    {
        await using var stream = SourceAssembly.GetManifestResourceStream(ResourcePath) ??
                                 throw new InvalidOperationException("Could not load contents of " + ResourcePath);

        return await new StreamReader(stream).ReadToEndAsync();
    }
}