using System.Reflection;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Interfaces;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

public class ManiaTemplateEngine
{
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage = new MtLanguageT4();
    private readonly Dictionary<string, MtComponent> _components = new();
    protected internal MtComponentMap BaseMtComponents { get; }

    private static readonly Regex NamespaceWrapperMatcher = new(@"namespace ManiaTemplate \{((?:.|\n)+)\}");

    public ManiaTemplateEngine()
    {
        BaseMtComponents = LoadCoreComponents();
    }

    /// <summary>
    /// Loads the components that are available by default.
    /// </summary>
    private MtComponentMap LoadCoreComponents()
    {
        return new MtComponentMap
        {
            { "Pagination", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.Components.Pagination.mt").GetComponent() },
            { "Graph", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.ManiaLink.Graph.mt").GetComponent() },
            { "Label", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.ManiaLink.Label.mt").GetComponent() },
            { "Quad", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.ManiaLink.Quad.mt").GetComponent() },
            { "RoundedQuad", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.ManiaLink.RoundedQuad.mt").GetComponent() },
            { "Frame", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.Wrapper.Frame.mt").GetComponent() },
            { "Widget", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.Wrapper.Widget.mt").GetComponent() },
            { "WindowTitleBar", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.Wrapper.Includes.WindowTitleBar.mt").GetComponent() },
            { "Window", LoadTemplateFromEmbeddedResource("ManiaTemplates.Templates.Wrapper.Window.mt").GetComponent() },
        };
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public IManiaTemplate LoadTemplateFromEmbeddedResource(string resourcePath)
    {
        var templateContent = Helper.GetEmbeddedResourceContent(resourcePath, Assembly.GetCallingAssembly()).Result;
        var component = MtComponent.FromTemplate(this, templateContent);
        _components.Add(resourcePath, component);

        return new LoadedTemplate
        {
            Engine = this,
            Component = component,
            SourceAssembly = Assembly.GetCallingAssembly(),
            ResourceKey = resourcePath
        };
    }

    /// <summary>
    /// Gets a component from the engine.
    /// </summary>
    public MtComponent GetComponent(string path)
    {
        return _components[path];
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    internal ManiaLink PreProcess(MtComponent mtComponent, Assembly assembly, string className, string? writeTo = null)
    {
        var t4Template = ConvertComponentToT4Template(mtComponent, className);
        var ttFilename = $"{className}.tt";

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = className;
        templateSettings.Namespace = "ManiaTemplate";

        if (writeTo != null)
        {
            File.WriteAllText(writeTo, t4Template);
        }

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        //Remove namespace wrapper
        preCompiledTemplate = NamespaceWrapperMatcher.Replace(preCompiledTemplate, "$1");

        return new ManiaLink(className, preCompiledTemplate, assembly);
    }

    /// <summary>
    /// Converts a component instance into a renderable instance.
    /// </summary>
    private string ConvertComponentToT4Template(MtComponent mtComponent, string className) =>
        new Transformer(this, _maniaTemplateLanguage).BuildManialink(mtComponent, className);

    /// <summary>
    /// Generates the contents for a markdown file describing all base components shipped with the template engine.
    /// </summary>
    public string GenerateComponentsMarkdown() =>
        new MtComponentMarkdownGenerator { Components = BaseMtComponents }.Generate();
}