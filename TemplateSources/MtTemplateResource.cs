using System.Reflection;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.TemplateSources;

public class MtTemplateResource : IMtTemplate
{
    public required string ResourcePath { get; init; }

    public async Task<string> GetContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(ResourcePath) ??
                                 throw new InvalidOperationException("Could not load contents of " + ResourcePath);

        return await new StreamReader(stream).ReadToEndAsync();
    }

    public string GetXmlTag() => ResourcePath.Split('.')[^2];

    public string? GetBasePath() => null;
}