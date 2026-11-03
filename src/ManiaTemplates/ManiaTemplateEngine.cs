using System.Collections.Concurrent;
using System.Reflection;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

/// <summary>
/// Renders templates to manialinks. The engine is thread-safe, so templates can be pre-processed and rendered in
/// parallel.
/// </summary>
public class ManiaTemplateEngine
{
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage = new MtLanguageT4();
    private readonly ConcurrentDictionary<string, MtComponent> _components = new();
    private readonly ConcurrentDictionary<string, string> _templates = new();
    private readonly ConcurrentDictionary<string, string> _maniaScripts = new();

    // Lazy makes sure a template is compiled once, even if it is rendered by multiple threads at the same time
    private readonly ConcurrentDictionary<string, Lazy<ManiaLink>> _preProcessed = new();
    public readonly ConcurrentDictionary<string, object?> GlobalVariables = new();
    protected internal MtComponentMap BaseMtComponents { get; }

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
            EmbeddedResourceUtils.GetEmbeddedResourceContent("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
                Assembly.GetExecutingAssembly()));

        return coreComponents;
    }

    /// <summary>
    /// Gets a MtComponent-instance for the given key.
    /// </summary>
    public MtComponent GetComponent(string key)
    {
        return _components.GetOrAdd(key, templateKey => MtComponent.FromTemplate(this, _templates[templateKey]));
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering.
    /// </summary>
    public Task PreProcessAsync(string key, IEnumerable<Assembly> assemblies)
    {
        // Compiling takes a while, running it on the thread pool allows pre-processing templates in parallel
        return Task.Run(() => PreProcess(key, assemblies, null));
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering and writes the generated T4 template to the given directory,
    /// which helps debugging templates.
    /// </summary>
    public Task PreProcessAsync(string key, IEnumerable<Assembly> assemblies, string writeTo)
    {
        return Task.Run(() => PreProcess(key, assemblies, writeTo));
    }

    private void PreProcess(string key, IEnumerable<Assembly> assemblies, string? writeTo)
    {
        var maniaLink = PreProcessComponent(GetComponent(key), ManialinkNameUtils.KeyToId(key), assemblies, writeTo);
        _preProcessed[key] = new Lazy<ManiaLink>(maniaLink);
    }

    /// <summary>
    /// Renders a template in the given context.
    /// </summary>
    public async Task<string> RenderAsync(string key, dynamic data, IEnumerable<Assembly> assemblies)
    {
        var maniaLink = await GetPreProcessedAsync(key, assemblies).ConfigureAwait(false);
        return await maniaLink.RenderAsync(data, GlobalVariables);
    }

    public async Task<string> RenderAsync(string key, IDictionary<string, object?> data,
        IEnumerable<Assembly> assemblies)
    {
        var maniaLink = await GetPreProcessedAsync(key, assemblies).ConfigureAwait(false);
        return await maniaLink.RenderAsync(data, GlobalVariables).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an empty manialink for a template-key, used to hide the rendered template from players.
    /// </summary>
    public string Hide(string key) => ManiaLink.CreateHideManialink(ManialinkNameUtils.KeyToId(key));

    /// <summary>
    /// Returns the pre-processed template, compiling it on the thread pool if it has not been compiled yet.
    /// </summary>
    private async Task<ManiaLink> GetPreProcessedAsync(string key, IEnumerable<Assembly> assemblies)
    {
        if (_preProcessed.TryGetValue(key, out var preProcessed) && preProcessed.IsValueCreated)
        {
            return preProcessed.Value;
        }

        return await Task.Run(() => GetPreProcessed(key, assemblies)).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the pre-processed template, compiling it if it has not been compiled yet.
    /// A failed compilation is not cached, so the template is compiled again on the next call.
    /// </summary>
    private ManiaLink GetPreProcessed(string key, IEnumerable<Assembly> assemblies)
    {
        var preProcessed = _preProcessed.GetOrAdd(key, templateKey => new Lazy<ManiaLink>(() =>
            PreProcessComponent(GetComponent(templateKey), ManialinkNameUtils.KeyToId(templateKey), assemblies,
                null)));

        try
        {
            return preProcessed.Value;
        }
        catch
        {
            _preProcessed.TryRemove(new KeyValuePair<string, Lazy<ManiaLink>>(key, preProcessed));
            throw;
        }
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void LoadTemplateFromEmbeddedResource(string resourcePath)
    {
        var templateContent =
            EmbeddedResourceUtils.GetEmbeddedResourceContent(resourcePath, Assembly.GetCallingAssembly());
        AddTemplateFromString(resourcePath, templateContent);
    }

    /// <summary>
    /// Used to add components for ManiaTemplates. These components may not be rendered individually.
    /// </summary>
    public void AddTemplateFromString(string templateKey, string templateContent)
    {
        if (!_templates.TryAdd(templateKey, templateContent))
        {
            throw new ArgumentException($"A template with the key '{templateKey}' has already been added.",
                nameof(templateKey));
        }
    }

    /// <summary>
    /// Adds a ManiaScript to be used in components, defined by a key.
    /// </summary>
    public void AddManiaScriptFromString(string scriptKey, string maniaScript)
    {
        if (!_maniaScripts.TryAdd(scriptKey, maniaScript))
        {
            throw new ManiaScriptAlreadyExistsException($"ManiaScript '{scriptKey}' already exists.");
        }
    }

    /// <summary>
    /// Gets the contents of a ManiaScript for the given key.
    /// </summary>
    public string GetManiaScript(string scriptKey)
    {
        return _maniaScripts.TryGetValue(scriptKey, out var maniaScript)
            ? maniaScript
            : throw new ManiaScriptNotFoundException($"ManiaScript '{scriptKey}' not found.");
    }

    /// <summary>
    /// Takes a MtComponent instance and prepares it for rendering.
    /// </summary>
    private ManiaLink PreProcessComponent(MtComponent mtComponent, string className, IEnumerable<Assembly> assemblies,
        string? writeTo)
    {
        var t4Template = ConvertComponentToT4Template(mtComponent, className);
        var ttFilename = $"{className}.tt";

        if (writeTo != null)
        {
            File.WriteAllText(Path.Combine(writeTo, className + ".tt"), t4Template);
        }

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = className;
        // Without namespace the generated class can be found by its name
        templateSettings.Namespace = "";

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        if (preCompiledTemplate == null)
        {
            throw new ManiaTemplatePreProcessingFailedException(
                $"Failed to pre-process '{mtComponent.TemplateContent}'.");
        }

        // Parse null to empty strings
        preCompiledTemplate =
            preCompiledTemplate.Replace(@"throw new global::System.ArgumentNullException(""objectToConvert"");",
                @"return """";");

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
        new MtComponentMarkdownGenerator { Engine = this, Components = BaseMtComponents }.Generate();

    public void RemoveTemplate(string name)
    {
        if (!_templates.TryRemove(name, out _))
        {
            throw new InvalidOperationException($"Template with name '{name}' does not exist.");
        }

        _components.TryRemove(name, out _);
        // Drops the compiled template, which lives in the load context of the assemblies it was rendered
        // against, so unloading that context isn't blocked by a cached reference.
        _preProcessed.TryRemove(name, out _);
    }

    public void RemoveManiaScript(string name)
    {
        if (!_maniaScripts.TryRemove(name, out _))
        {
            throw new InvalidOperationException($"ManiaScript with name '{name}' does not exist.");
        }
    }
}