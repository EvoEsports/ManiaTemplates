using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
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
    private readonly Dictionary<string, string> _maniaScripts = new();
    private readonly ConcurrentDictionary<string, ManiaLink> _preProcessed = new();
    protected internal MtComponentMap BaseMtComponents { get; }

    private static readonly Regex NamespaceWrapperMatcher = new(@"namespace ManiaTemplates \{((?:.|\n)+)\}");

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

        AddManiaScriptFromString("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
            Helper.GetEmbeddedResourceContentAsync("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
                Assembly.GetExecutingAssembly()).Result);

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
    public void PreProcess(string key, IEnumerable<Assembly> assemblies)
    {
        _preProcessed[key] = PreProcessComponent(GetComponent(key), ManialinkNameUtils.KeyToId(key), assemblies);
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering.
    /// </summary>
    public async Task PreProcessAsync(string key, IEnumerable<Assembly> assemblies)
    {
        _preProcessed[key] =
            await PreProcessComponentAsync(GetComponent(key), ManialinkNameUtils.KeyToId(key), assemblies);
    }

    /// <summary>
    /// Renders a template in the given context.
    /// </summary>
    public async Task<string> RenderAsync(string key, dynamic data, IEnumerable<Assembly> assemblies)
    {
        await CheckPreprocessingAsync(key, assemblies);
        return await _preProcessed[key].RenderAsync(data);
    }

    public async Task<string> RenderAsync(string key, IDictionary<string, object?> data,
        IEnumerable<Assembly> assemblies)
    {
        await CheckPreprocessingAsync(key, assemblies);
        return await _preProcessed[key].RenderAsync(data);
    }

    private async Task CheckPreprocessingAsync(string key, IEnumerable<Assembly> assemblies)
    {
        var assemblyList = assemblies.ToList();
        if (!_preProcessed.ContainsKey(key))
        {
            await PreProcessAsync(key, assemblyList);
        }
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void LoadTemplateFromEmbeddedResource(string resourcePath)
    {
        var templateContent =
            Helper.GetEmbeddedResourceContentAsync(resourcePath, Assembly.GetCallingAssembly()).Result;
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
    /// Adds a ManiaScript to be used in components, defined by a key.
    /// </summary>
    public void AddManiaScriptFromString(string scriptKey, string maniaScript)
    {
        if (_maniaScripts.ContainsKey(scriptKey))
        {
            throw new ManiaScriptAlreadyExistsException($"ManiaScript '{scriptKey}' already exists.");
        }

        _maniaScripts.Add(scriptKey, maniaScript);
    }

    /// <summary>
    /// Gets the contents of a ManiaScript for the given key.
    /// </summary>
    public string GetManiaScript(string scriptKey)
    {
        if (!_maniaScripts.ContainsKey(scriptKey))
        {
            throw new ManiaScriptNotFoundException($"ManiaScript '{scriptKey}' not found.");
        }

        return _maniaScripts[scriptKey];
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    private ManiaLink PreProcessComponent(MtComponent mtComponent, string className, IEnumerable<Assembly> assemblies,
        string? writeTo = null)
    {
        return PreProcessComponentAsync(mtComponent, className, assemblies, writeTo).Result;
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    private async Task<ManiaLink> PreProcessComponentAsync(MtComponent mtComponent, string className,
        IEnumerable<Assembly> assemblies,
        string? writeTo = null)
    {
        var t4Template = ConvertComponentToT4Template(mtComponent, className);
        var ttFilename = $"{className}.tt";

        if (writeTo != null)
        {
            await File.WriteAllTextAsync(Path.Combine(writeTo,className + ".tt"), t4Template);
        }

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = className;
        templateSettings.Namespace = "ManiaTemplates";

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        if (preCompiledTemplate == null)
        {
            throw new ManiaTemplatePreProcessingFailedException(
                $"Failed to pre-process '{mtComponent.TemplateContent}'.");
        }

        //Remove namespace wrapper
        preCompiledTemplate = NamespaceWrapperMatcher.Replace(preCompiledTemplate, "$1");

        //Parse null to empty strings
        preCompiledTemplate =
            preCompiledTemplate.Replace(@"throw new global::System.ArgumentNullException(""objectToConvert"");",
                @"return """";");

        if (writeTo != null)
        {
            // await File.WriteAllTextAsync(writeTo + ".cs", preCompiledTemplate);
        }

        return new ManiaLink(className, preCompiledTemplate, assemblies);
    }

    /// <summary>
    /// Converts a component instance into a renderable instance.
    /// </summary>
    private string ConvertComponentToT4Template(MtComponent mtComponent, string className) =>
        new MtTransformer(this, _maniaTemplateLanguage).BuildManialink(mtComponent, className);

    /// <summary>
    /// Generates the contents for a markdown file describing all base components shipped with the template engine.
    /// </summary>
    public string GenerateComponentsMarkdown() =>
        new MtComponentMarkdownGenerator { Components = BaseMtComponents }.Generate();

    public void RemoveTemplate(string name)
    {
        if (!_templates.ContainsKey(name))
        {
            throw new InvalidOperationException($"Template with name '{name}' does not exist.");
        }

        _templates.Remove(name);
        _components.Remove(name);
    }

    public void RemoveManiaScript(string name)
    {
        if (!_maniaScripts.ContainsKey(name))
        {
            throw new InvalidOperationException($"ManiaScript with name '{name}' does not exist.");
        }

        _maniaScripts.Remove(name);
    }
}