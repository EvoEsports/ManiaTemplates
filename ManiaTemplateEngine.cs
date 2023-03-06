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
    private readonly Dictionary<string, string> _templates = new();
    private readonly Dictionary<string, ManiaLink> _preProcessed = new();
    protected internal MtComponentMap BaseMtComponents { get; }

    private static readonly Regex NamespaceWrapperMatcher = new(@"namespace ManiaTemplates \{((?:.|\n)+)\}");
    private static readonly Regex ClassNameSlugifier = new(@"(?:[^a-zA-Z0-9]|mt$|xml$)");

    public ManiaTemplateEngine()
    {
        BaseMtComponents = LoadCoreComponents();
    }

    /// <summary>
    /// Loads the components that are available by default.
    /// </summary>
    private MtComponentMap LoadCoreComponents()
    {
        var coreComponents = new MtComponentMap
        {
            {
                "Pagination",
                new MtComponentImport
                    { TemplateKey = "ManiaTemplates.Templates.Components.Pagination.mt", Tag = "Pagination" }
            },
            {
                "Graph",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.ManiaLink.Graph.mt", Tag = "Graph" }
            },
            {
                "Label",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.ManiaLink.Label.mt", Tag = "Label" }
            },
            {
                "Quad",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.ManiaLink.Quad.mt", Tag = "Quad" }
            },
            {
                "RoundedQuad",
                new MtComponentImport
                    { TemplateKey = "ManiaTemplates.Templates.ManiaLink.RoundedQuad.mt", Tag = "RoundedQuad" }
            },
            {
                "Frame",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.Wrapper.Frame.mt", Tag = "Frame" }
            },
            {
                "Widget",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.Wrapper.Widget.mt", Tag = "Widget" }
            },
            {
                "WindowTitleBar",
                new MtComponentImport
                {
                    TemplateKey = "ManiaTemplates.Templates.Wrapper.Includes.WindowTitleBar.mt", Tag = "WindowTitleBar"
                }
            },
            {
                "Window",
                new MtComponentImport { TemplateKey = "ManiaTemplates.Templates.Wrapper.Window.mt", Tag = "Window" }
            }
        };

        foreach (var coreComponent in coreComponents.Values)
        {
            LoadTemplateFromEmbeddedResource(coreComponent.TemplateKey);
        }

        return coreComponents;
    }

    /// <summary>
    /// Gets a MtComponent-instance for the given key.
    /// </summary>
    public MtComponent GetComponent(string key)
    {
        if (_components.ContainsKey(key))
        {
            return _components[key];
        }

        var component = MtComponent.FromTemplate(this, _templates[key]);
        _components.Add(key, component);

        return component;
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering.
    /// </summary>
    public void PreProcess(string key, Assembly assembly)
    {
        _preProcessed[key] = PreProcessComponent(GetComponent(key), assembly, KeyToClassName(key));
    }

    /// <summary>
    /// Renders a template in the given context.
    /// </summary>
    public string Render(string key, dynamic data, Assembly executionContext)
    {
        if (!_preProcessed.ContainsKey(key))
        {
            PreProcess(key, executionContext);
        }

        return _preProcessed[key].Render(data, executionContext);
    }

    /// <summary>
    /// Converts the template-key to a safe c#-class name.
    /// </summary>
    private string KeyToClassName(string key)
    {
        return "Mt" + ClassNameSlugifier.Replace(key, "");
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void LoadTemplateFromEmbeddedResource(string resourcePath)
    {
        var templateContent = Helper.GetEmbeddedResourceContentAsync(resourcePath, Assembly.GetCallingAssembly()).Result;
        AddTemplateFromString(resourcePath, templateContent);
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void AddTemplateFromString(string templateKey, string templateContent)
    {
        _templates.Add(templateKey, templateContent);
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    private ManiaLink PreProcessComponent(MtComponent mtComponent, Assembly assembly, string className,
        string? writeTo = null)
    {
        var t4Template = ConvertComponentToT4Template(mtComponent, className);
        var ttFilename = $"{className}.tt";

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = className;
        templateSettings.Namespace = "ManiaTemplates";

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        // foreach (var ass in loadedAssemblies)
        // {
        //     Console.WriteLine("Loaded assembly:" + ass);
        // }

        //Remove namespace wrapper
        preCompiledTemplate = NamespaceWrapperMatcher.Replace(preCompiledTemplate, "$1");

        if (writeTo != null)
        {
            File.WriteAllText(writeTo + ".tt", t4Template);
            File.WriteAllText(writeTo + ".cs", preCompiledTemplate);
        }

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