using System.Collections.Concurrent;
using System.Reflection;
using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

/// <summary>
/// Renders templates to manialinks, implemented by <see cref="ManiaTemplateEngine"/>. Allows consumers to mock the
/// engine.
/// </summary>
public interface IManiaTemplateEngine
{
    /// <summary>
    /// Variables available in all templates, like services or settings. They are added to templates when they are
    /// compiled, so templates compiled before a variable was added can't use it.
    /// </summary>
    ConcurrentDictionary<string, object?> GlobalVariables { get; }

    /// <summary>
    /// Gets a MtComponent-instance for the given key.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    MtComponent GetComponent(string key);

    /// <summary>
    /// PreProcesses a template-key for faster rendering, cancelling the token stops the compilation.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    Task PreProcessAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// PreProcesses a template-key for faster rendering and writes the generated T4 template to the given directory,
    /// which helps debugging templates.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="writeTo">The directory the T4 template is written to.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    Task PreProcessAsync(string key, string writeTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a template with the given data, an object with properties or a dictionary. The template is compiled
    /// first, if it has not been compiled yet, cancelling the token stops the compilation.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    /// <param name="data">The values of the template properties, an object with properties or a dictionary.</param>
    /// <param name="cancellationToken">Stops the compilation.</param>
    Task<string> RenderAsync(string key, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an empty manialink for a template-key, used to hide the rendered template from players.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    string Hide(string key);

    /// <summary>
    /// Adds a template from an embedded resource of the given assembly, which is compiled into the load context of that
    /// assembly. The assembly is passed explicitly, as the calling assembly is unreliable: in release builds the JIT may
    /// turn the call into a tail call, which removes the frame of the caller.
    /// </summary>
    /// <param name="resourcePath">The name of the embedded resource, which is also the key of the template.</param>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    void LoadTemplateFromEmbeddedResource(string resourcePath, Assembly assembly);

    /// <summary>
    /// Adds a template, which is compiled into the default load context and can use the types of the host.
    /// </summary>
    /// <param name="templateKey">The key the template is rendered and imported with.</param>
    /// <param name="templateContent">The contents of the template.</param>
    void AddTemplateFromString(string templateKey, string templateContent);

    /// <summary>
    /// Adds a template loaded from the given assembly, like a template of a plugin. It is compiled into the load
    /// context of the assembly and can use the types of the assembly, of the referenced assemblies, of its load context
    /// and of the host. The template is removed, when the load context of one of the assemblies unloads.
    /// </summary>
    /// <param name="templateKey">The key the template is rendered and imported with.</param>
    /// <param name="templateContent">The contents of the template.</param>
    /// <param name="assembly">The assembly the template was loaded from.</param>
    /// <param name="references">Other assemblies containing types the template uses.</param>
    void AddTemplateFromString(string templateKey, string templateContent, Assembly assembly,
        params IEnumerable<Assembly> references);

    /// <summary>
    /// Adds a ManiaScript to be used in components, defined by a key.
    /// </summary>
    /// <param name="scriptKey">The key the script is used with in the resource attribute.</param>
    /// <param name="maniaScript">The ManiaScript code.</param>
    void AddManiaScriptFromString(string scriptKey, string maniaScript);

    /// <summary>
    /// Gets the contents of a ManiaScript for the given key.
    /// </summary>
    /// <param name="scriptKey">The key of the script.</param>
    string GetManiaScript(string scriptKey);

    /// <summary>
    /// Generates the contents for a markdown file describing all base components shipped with the template engine.
    /// </summary>
    string GenerateComponentsMarkdown();

    /// <summary>
    /// Removes a template, together with its compiled version and the compiled templates importing it.
    /// </summary>
    /// <param name="name">The key of the template.</param>
    void RemoveTemplate(string name);

    /// <summary>
    /// Removes a ManiaScript. Compiled templates keep the content they were compiled with.
    /// </summary>
    /// <param name="name">The key of the script.</param>
    void RemoveManiaScript(string name);
}
