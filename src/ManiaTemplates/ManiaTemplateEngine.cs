using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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
    private readonly ConcurrentDictionary<string, RegisteredTemplate> _templates = new();
    private readonly ConcurrentDictionary<string, string> _maniaScripts = new();

    // Compiled templates are dropped when a template they were built from is removed or replaced, or when one of their
    // load contexts unloads, as they would keep it alive
    private readonly ConcurrentDictionary<string, CompiledTemplate> _compiledTemplates = new();

    // The tables do not keep the load contexts alive, as their values do not reference the contexts
    private readonly ConditionalWeakTable<AssemblyLoadContext, object> _watchedLoadContexts = new();
    private readonly ConditionalWeakTable<AssemblyLoadContext, object> _unloadingLoadContexts = new();

    /// <summary>
    /// A template with the assembly it was loaded from, which decides the load context it is compiled into, and the
    /// assemblies containing the types it uses.
    /// </summary>
    /// <param name="Content">The contents of the template.</param>
    /// <param name="Assembly">The assembly the template was loaded from, null for templates of the host.</param>
    /// <param name="References">The assemblies containing the types the template uses.</param>
    internal sealed record RegisteredTemplate(string Content, Assembly? Assembly, IReadOnlyList<Assembly> References)
    {
        /// <summary>
        /// The assembly of the template followed by its references.
        /// </summary>
        public IEnumerable<Assembly> Assemblies => Assembly == null ? References : References.Prepend(Assembly);
    }

    /// <summary>
    /// A compiled template with the templates it was built from, its own and the ones of all imported components, and
    /// the collectible load contexts of their assemblies.
    /// </summary>
    private sealed record CompiledTemplate(ManiaLink ManiaLink, IReadOnlyDictionary<string, RegisteredTemplate> Templates,
        IReadOnlyList<AssemblyLoadContext> LoadContexts);

    /// <summary>
    /// Variables available in all templates, like services or settings. They are added to templates when they are
    /// compiled, so templates compiled before a variable was added can't use it.
    /// </summary>
    public readonly ConcurrentDictionary<string, object?> GlobalVariables = new();

    /// <summary>
    /// The core components available in all templates.
    /// </summary>
    protected internal MtComponentMap BaseMtComponents { get; }

    /// <summary>
    /// Creates an engine with the core components loaded.
    /// </summary>
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
            LoadTemplateFromEmbeddedResource(coreComponent.TemplateKey, typeof(ManiaTemplateEngine).Assembly);
        }

        AddManiaScriptFromString("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
            EmbeddedResourceUtils.GetEmbeddedResourceContent("ManiaTemplates.ManiaScripts.Wrapper.Window.ms",
                Assembly.GetExecutingAssembly()));

        return coreComponents;
    }

    /// <summary>
    /// Gets a MtComponent-instance for the given key.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    public MtComponent GetComponent(string key) => MtComponent.FromTemplate(this, _templates[key].Content);

    /// <summary>
    /// Gets a MtComponent-instance for the given key, together with the template it was created from.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    internal (RegisteredTemplate Template, MtComponent Component) GetComponentWithTemplate(string key)
    {
        var template = _templates[key];
        return (template, MtComponent.FromTemplate(this, template.Content));
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering, cancelling the token stops the compilation.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    public Task PreProcessAsync(string key, CancellationToken cancellationToken = default)
    {
        // Compiling takes a while, running it on the thread pool allows pre-processing templates in parallel
        return Task.Run(() => PreProcess(key, null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// PreProcesses a template-key for faster rendering and writes the generated T4 template to the given directory,
    /// which helps debugging templates.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="writeTo">The directory the T4 template is written to.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    public Task PreProcessAsync(string key, string writeTo, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => PreProcess(key, writeTo, cancellationToken), cancellationToken);
    }

    private ManiaLink PreProcess(string key, string? writeTo, CancellationToken cancellationToken)
    {
        var compiled = Compile(key, _templates[key], writeTo, cancellationToken);
        _compiledTemplates[key] = compiled;

        // Checked after adding, as a template may be removed or a load context may unload at the same time
        if (!IsCurrent(compiled))
        {
            _compiledTemplates.TryRemove(new KeyValuePair<string, CompiledTemplate>(key, compiled));
        }

        return compiled.ManiaLink;
    }

    /// <summary>
    /// Renders a template with the given data, an object with properties or a dictionary. The template is compiled
    /// first, if it has not been compiled yet, cancelling the token stops the compilation.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="data">The values of the template properties, an object with properties or a dictionary.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    public async Task<string> RenderAsync(string key, object data, CancellationToken cancellationToken = default)
    {
        var maniaLink = _compiledTemplates.TryGetValue(key, out var compiled) && IsCurrent(compiled)
            ? compiled.ManiaLink
            : await Task.Run(() => PreProcess(key, null, cancellationToken), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // A type check instead of dynamic dispatch, as the runtime binder caches the types it is called with, which
        // would keep collectible load contexts alive
        var rendering = data is IDictionary<string, object?> dictionary
            ? maniaLink.RenderAsync(dictionary, GlobalVariables)
            : maniaLink.RenderAsync(data, GlobalVariables);

        return await rendering.ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an empty manialink for a template-key, used to hide the rendered template from players.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    public string Hide(string key) => ManiaLink.CreateHideManialink(ManialinkNameUtils.KeyToId(key));

    /// <summary>
    /// Adds a template from an embedded resource of the given assembly, which is compiled into the load context of that
    /// assembly. The assembly is passed explicitly, as the calling assembly is unreliable: in release builds the JIT may
    /// turn the call into a tail call, which removes the frame of the caller.
    /// </summary>
    /// <param name="resourcePath">The name of the embedded resource, which is also the key of the template.</param>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    public void LoadTemplateFromEmbeddedResource(string resourcePath, Assembly assembly)
    {
        AddTemplate(resourcePath,
            new RegisteredTemplate(EmbeddedResourceUtils.GetEmbeddedResourceContent(resourcePath, assembly), assembly,
                []));
    }

    /// <summary>
    /// Adds a template, which is compiled into the default load context and can use the types of the host.
    /// </summary>
    /// <param name="templateKey">The key the template is rendered and imported with.</param>
    /// <param name="templateContent">The contents of the template.</param>
    public void AddTemplateFromString(string templateKey, string templateContent)
    {
        AddTemplate(templateKey, new RegisteredTemplate(templateContent, null, []));
    }

    /// <summary>
    /// Adds a template loaded from the given assembly, like a template of a plugin. It is compiled into the load
    /// context of the assembly and can use the types of the assembly, of the referenced assemblies, of its load context
    /// and of the host. The template is removed, when the load context of one of the assemblies unloads.
    /// </summary>
    /// <param name="templateKey">The key the template is rendered and imported with.</param>
    /// <param name="templateContent">The contents of the template.</param>
    /// <param name="assembly">The assembly the template was loaded from.</param>
    /// <param name="references">Other assemblies containing types the template uses.</param>
    public void AddTemplateFromString(string templateKey, string templateContent, Assembly assembly,
        params IEnumerable<Assembly> references)
    {
        AddTemplate(templateKey, new RegisteredTemplate(templateContent, assembly, references.ToList()));
    }

    private void AddTemplate(string templateKey, RegisteredTemplate template)
    {
        if (!_templates.TryAdd(templateKey, template))
        {
            throw new ArgumentException($"A template with the key '{templateKey}' has already been added.",
                nameof(templateKey));
        }

        WatchLoadContexts(template.Assemblies);

        // The load context may have started unloading while the template was added
        if (GetLoadContexts(template.Assemblies).Any(IsUnloading))
        {
            RemoveTemplateAndCompilations(templateKey, template);
        }
    }

    /// <summary>
    /// Adds a ManiaScript to be used in components, defined by a key.
    /// </summary>
    /// <param name="scriptKey">The key the script is used with in the resource attribute.</param>
    /// <param name="maniaScript">The ManiaScript code.</param>
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
    /// <param name="scriptKey">The key of the script.</param>
    public string GetManiaScript(string scriptKey)
    {
        return _maniaScripts.TryGetValue(scriptKey, out var maniaScript)
            ? maniaScript
            : throw new ManiaScriptNotFoundException($"ManiaScript '{scriptKey}' not found.");
    }

    /// <summary>
    /// Compiles the given template of the key, together with the templates of all components it imports, into the
    /// load context of the template's assembly.
    /// </summary>
    private CompiledTemplate Compile(string key, RegisteredTemplate template, string? writeTo,
        CancellationToken cancellationToken)
    {
        var mtComponent = MtComponent.FromTemplate(this, template.Content);
        var className = ManialinkNameUtils.KeyToId(key);
        var transformer = new MtTransformer(this, _maniaTemplateLanguage);
        var t4Template = transformer.BuildManialink(mtComponent, className);
        var ttFilename = $"{className}.tt";

        if (writeTo != null)
        {
            File.WriteAllText(Path.Combine(writeTo, className + ".tt"), t4Template);
        }

        cancellationToken.ThrowIfCancellationRequested();
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

        // The imported components are part of the compiled template, so it uses their types as well
        var templates = new Dictionary<string, RegisteredTemplate>(transformer.ImportedTemplates) { [key] = template };
        var assemblies = templates.Values.SelectMany(usedTemplate => usedTemplate.Assemblies).Distinct().ToList();
        var loadContext = template.Assembly == null
            ? AssemblyLoadContext.Default
            : AssemblyLoadContext.GetLoadContext(template.Assembly) ?? AssemblyLoadContext.Default;

        cancellationToken.ThrowIfCancellationRequested();
        var maniaLink = new ManiaLink(className, preCompiledTemplate, loadContext, assemblies, cancellationToken);

        return new CompiledTemplate(maniaLink, templates, GetLoadContexts(assemblies).ToList());
    }

    /// <summary>
    /// Checks whether the template was compiled from the current templates and none of its load contexts is unloading.
    /// </summary>
    private bool IsCurrent(CompiledTemplate compiled) =>
        compiled.Templates.All(template =>
            _templates.TryGetValue(template.Key, out var current) && ReferenceEquals(current, template.Value)) &&
        !compiled.LoadContexts.Any(IsUnloading);

    /// <summary>
    /// Returns the collectible load contexts of the assemblies, which may unload.
    /// </summary>
    private static IEnumerable<AssemblyLoadContext> GetLoadContexts(IEnumerable<Assembly> assemblies) =>
        assemblies.Select(AssemblyLoadContext.GetLoadContext)
            .OfType<AssemblyLoadContext>()
            .Where(loadContext => loadContext.IsCollectible)
            .Distinct();

    /// <summary>
    /// Watches the collectible load contexts of the assemblies, to remove the templates using them when they unload.
    /// </summary>
    private void WatchLoadContexts(IEnumerable<Assembly> assemblies)
    {
        foreach (var loadContext in GetLoadContexts(assemblies))
        {
            if (!_watchedLoadContexts.TryAdd(loadContext, new object()))
            {
                continue;
            }

            // The load context only references the engine weakly, so it does not keep a discarded engine alive.
            // A context that is not unloaded explicitly raises the event from its finalizer, where the engine may be
            // collected already.
            var engine = new WeakReference<ManiaTemplateEngine>(this);
            loadContext.Unloading += context =>
            {
                if (engine.TryGetTarget(out var target))
                {
                    target.RemoveUnloadingLoadContext(context);
                }
            };
        }
    }

    private bool IsUnloading(AssemblyLoadContext loadContext) => _unloadingLoadContexts.TryGetValue(loadContext, out _);

    /// <summary>
    /// Removes the templates using the assemblies of an unloading load context, and the templates compiled with them.
    /// </summary>
    private void RemoveUnloadingLoadContext(AssemblyLoadContext loadContext)
    {
        _unloadingLoadContexts.AddOrUpdate(loadContext, new object());

        foreach (var (key, template) in _templates)
        {
            if (GetLoadContexts(template.Assemblies).Contains(loadContext))
            {
                RemoveTemplateAndCompilations(key, template);
            }
        }

        RemoveCompilations(compiled => compiled.LoadContexts.Contains(loadContext));
    }

    private void RemoveTemplateAndCompilations(string key, RegisteredTemplate template)
    {
        _templates.TryRemove(new KeyValuePair<string, RegisteredTemplate>(key, template));
        RemoveCompilations(compiled => compiled.Templates.ContainsKey(key));
    }

    private void RemoveCompilations(Func<CompiledTemplate, bool> predicate)
    {
        foreach (var pair in _compiledTemplates)
        {
            if (predicate(pair.Value))
            {
                _compiledTemplates.TryRemove(pair);
            }
        }
    }

    /// <summary>
    /// Generates the contents for a markdown file describing all base components shipped with the template engine.
    /// </summary>
    public string GenerateComponentsMarkdown() =>
        new MtComponentMarkdownGenerator { Engine = this, Components = BaseMtComponents }.Generate();

    /// <summary>
    /// Removes a template, together with its compiled version and the compiled templates importing it.
    /// </summary>
    /// <param name="name">The key of the template.</param>
    public void RemoveTemplate(string name)
    {
        if (!_templates.TryGetValue(name, out var template))
        {
            throw new InvalidOperationException($"Template with name '{name}' does not exist.");
        }

        RemoveTemplateAndCompilations(name, template);
    }

    /// <summary>
    /// Removes a ManiaScript. Compiled templates keep the content they were compiled with.
    /// </summary>
    /// <param name="name">The key of the script.</param>
    public void RemoveManiaScript(string name)
    {
        if (!_maniaScripts.TryRemove(name, out _))
        {
            throw new InvalidOperationException($"ManiaScript with name '{name}' does not exist.");
        }
    }
}