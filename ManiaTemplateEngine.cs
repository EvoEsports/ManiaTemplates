using System.Reflection;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

public class ManiaTemplateEngine
{
    private readonly IMtLanguage _mtLanguage = new MtLanguageT4();
    protected internal MtComponentList BaseMtComponents { get; } = LoadCoreComponents();

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    public ManiaLink PreProcess(MtComponent mtComponent, string? writeTo = null)
    {
        var t4Template = ConvertComponent(mtComponent);
        var ttFilename = $"{mtComponent.Tag}.tt";

        File.WriteAllText(ttFilename, t4Template);

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = mtComponent.Tag;
        templateSettings.Namespace = "ManiaTemplate";

        if (writeTo != null)
        {
            File.WriteAllText(writeTo, t4Template);
        }

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        //Remove namespace wrapper
        preCompiledTemplate = preCompiledTemplate.Replace("namespace ManiaTemplate {", "")[..^5];

        return new ManiaLink(mtComponent.Tag, preCompiledTemplate, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Loads the components that are available by default.
    /// </summary>
    private static MtComponentList LoadCoreComponents()
    {
        var components = new MtComponentList();
        
        components.AddResource("ManiaTemplates.Templates.Components.Pagination.mt");
        components.AddResource("ManiaTemplates.Templates.ManiaLink.Graph.mt");
        components.AddResource("ManiaTemplates.Templates.ManiaLink.Label.mt");
        components.AddResource("ManiaTemplates.Templates.ManiaLink.Quad.mt");
        components.AddResource("ManiaTemplates.Templates.ManiaLink.RoundedQuad.mt");
        components.AddResource("ManiaTemplates.Templates.Wrapper.Frame.mt");
        components.AddResource("ManiaTemplates.Templates.Wrapper.Widget.mt");
        components.AddResource("ManiaTemplates.Templates.Wrapper.Window.mt");

        return components;
    }

    private string ConvertComponent(MtComponent mtComponent) =>
        new Transformer(this, _mtLanguage).BuildManialink(mtComponent);

    public string GenerateComponentsMarkdown() =>
        new MtComponentMarkdownGenerator { Components = BaseMtComponents }.Generate();
}