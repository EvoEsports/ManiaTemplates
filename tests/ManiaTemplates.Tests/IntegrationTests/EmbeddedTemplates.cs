namespace ManiaTemplates.Tests.IntegrationTests;

/// <summary>
/// Loads the embedded templates of this assembly. Called through a copy of the assembly in another load context, the
/// engine sees that copy as the calling assembly.
/// </summary>
public static class EmbeddedTemplates
{
    public const string LabelResource = "ManiaTemplates.Tests.IntegrationTests.templates.embedded.embedded-label.mt";

    public static void Load(ManiaTemplateEngine engine) => engine.LoadTemplateFromEmbeddedResource(LabelResource);
}
