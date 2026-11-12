namespace ManiaTemplates.Tests.IntegrationTests;

/// <summary>
/// Loads the embedded templates of this assembly. Called through a copy of the assembly in another load context, the
/// templates of that copy are loaded into its load context.
/// </summary>
public static class EmbeddedTemplates
{
    public const string LabelResource = "ManiaTemplates.Tests.IntegrationTests.templates.embedded.embedded-label.mt";

    public static void Load(ManiaTemplateEngine engine) =>
        engine.LoadTemplateFromEmbeddedResource(LabelResource, typeof(EmbeddedTemplates).Assembly);
}
