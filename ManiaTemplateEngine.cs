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
    private readonly IMtLanguage _mtLanguage = new MtLanguageT4();
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
        //TODO: automatically load contents of Templates
        AddComponentFromResource("ManiaTemplates.Templates.Components.Pagination.mt");
        AddComponentFromResource("ManiaTemplates.Templates.ManiaLink.Graph.mt");
        AddComponentFromResource("ManiaTemplates.Templates.ManiaLink.Label.mt");
        AddComponentFromResource("ManiaTemplates.Templates.ManiaLink.Quad.mt");
        AddComponentFromResource("ManiaTemplates.Templates.ManiaLink.RoundedQuad.mt");
        AddComponentFromResource("ManiaTemplates.Templates.Wrapper.Frame.mt");
        AddComponentFromResource("ManiaTemplates.Templates.Wrapper.Widget.mt");
        AddComponentFromResource("ManiaTemplates.Templates.Wrapper.Window.mt");
        AddComponentFromResource("ManiaTemplates.Templates.Wrapper.Includes.WindowTitleBar.mt");

        return new MtComponentMap
        {
            { "Pagination", "ManiaTemplates.Templates.Components.Pagination.mt" },
            { "Graph", "ManiaTemplates.Templates.ManiaLink.Graph.mt" },
            { "Label", "ManiaTemplates.Templates.ManiaLink.Label.mt" },
            { "Quad", "ManiaTemplates.Templates.ManiaLink.Quad.mt" },
            { "RoundedQuad", "ManiaTemplates.Templates.ManiaLink.RoundedQuad.mt" },
            { "Frame", "ManiaTemplates.Templates.Wrapper.Frame.mt" },
            { "Widget", "ManiaTemplates.Templates.Wrapper.Widget.mt" },
            { "Window", "ManiaTemplates.Templates.Wrapper.Window.mt" }
        };
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void AddComponent(IMtTemplate template, string importAs)
    {
        _components.Add(importAs, MtComponent.FromTemplate(this, template.GetContent().Result));
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void AddComponentFromResource(string resourcePath)
    {
        var templateContent = Helper.GetEmbeddedResourceContent(resourcePath, Assembly.GetCallingAssembly()).Result;
        var component = MtComponent.FromTemplate(this, templateContent);
        _components.Add(resourcePath, component);
    }

    /// <summary>
    /// Gets a component from the engine.
    /// </summary>
    public MtComponent GetComponent(string path)
    {
        return _components[path];
    }

    /// <summary>
    /// Used to add renderable ManiaLinks. These templates are pre-compiled for fast rendering.
    /// </summary>
    public ManiaLink CreateManiaLink(IMtTemplate template)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var className = NamespaceToClassName(callingAssembly.GetName().FullName);

        return PreProcess(MtComponent.FromTemplate(this, template.GetContent().Result), callingAssembly, className);
    }

    /// <summary>
    /// Used to add renderable ManiaLinks. These templates are pre-compiled for fast rendering.
    /// </summary>
    public ManiaLink CreateManiaLinkFromResource(string resourcePath)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var templateContent = Helper.GetEmbeddedResourceContent(resourcePath, callingAssembly);
        var className = NamespaceToClassName(resourcePath, 1);

        return PreProcess(MtComponent.FromTemplate(this, templateContent.Result), callingAssembly, className);
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    private ManiaLink PreProcess(MtComponent mtComponent, Assembly assembly, string className, string? writeTo = null)
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

    private static string NamespaceToClassName(string nameSpace, int skipLastNth = 0)
    {
        return string.Join("", nameSpace.Split('.')[..^skipLastNth]);
    }

    /// <summary>
    /// Converts a component instance into a renderable instance.
    /// </summary>
    private string ConvertComponentToT4Template(MtComponent mtComponent, string className) =>
        new Transformer(this, _mtLanguage).BuildManialink(mtComponent, className);

    /// <summary>
    /// Generates the contents for a markdown file describing all base components shipped with the template engine.
    /// </summary>
    public string GenerateComponentsMarkdown() =>
        new MtComponentMarkdownGenerator { Components = BaseMtComponents }.Generate(this);
}