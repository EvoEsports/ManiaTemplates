using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Loader;
using ManiaTemplates.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ManiaTemplates.Lib;

/// <summary>
/// Compiles pre-processed ManiaTemplates template classes into assemblies that live in the
/// <see cref="AssemblyLoadContext"/> of the template.
/// </summary>
/// <remarks>
/// A template uses the types of its own assembly and of the assemblies it references (module settings, models, ...).
/// Those types may live in a collectible load context. Emitting the compiled template into the context of the
/// template's assembly keeps the type identities intact and lets both be unloaded together.
/// </remarks>
internal static class TemplateCompiler
{
    private static int _assemblyCounter;

    // Namespaces every template can use without importing them
    private static readonly string[] DefaultUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks"
    ];

    // Files of the default load context never change, so their references are cached by path
    private static readonly ConcurrentDictionary<string, MetadataReference> FileReferenceCache = new();

    // A collectible load context may be loaded again with another version of its files, so its references are cached
    // per context. Keyed weakly, as a strong key would keep every context that compiled a template alive.
    private static readonly ConditionalWeakTable<AssemblyLoadContext, ConcurrentDictionary<string, MetadataReference>>
        LoadContextReferenceCache = new();

    /// <summary>
    /// Compiles the given pre-compiled template into the given load context and returns the type declared by
    /// <paramref name="className"/>. The template can use the types of the given assemblies, of the load context and
    /// of the host.
    /// </summary>
    public static Type Compile(string className, string preCompiledTemplate, AssemblyLoadContext loadContext,
        IReadOnlyCollection<Assembly> assemblies, CancellationToken cancellationToken)
    {

        var syntaxTree = CSharpSyntaxTree.ParseText(preCompiledTemplate,
            new CSharpParseOptions(LanguageVersion.Latest), cancellationToken: cancellationToken);

        var compilation = CSharpCompilation.Create(
            $"ManiaTemplates.Templates.{className}.{Interlocked.Increment(ref _assemblyCounter)}",
            [syntaxTree],
            BuildReferences(loadContext, assemblies),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true,
                usings: DefaultUsings));

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, cancellationToken: cancellationToken);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());

            throw new ManiaTemplateCompilationFailedException(
                $"Failed to compile template '{className}':{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        peStream.Position = 0;

        Assembly compiled;
        try
        {
            // Loading into the context of the template is what keeps the types the template binds to identical to
            // the ones of the template's assemblies.
            compiled = loadContext.LoadFromStream(peStream);
        }
        catch (Exception ex)
        {
            throw new ManiaTemplateCompilationFailedException(
                $"Failed to load compiled template '{className}' into load context '{loadContext.Name}'.", ex);
        }

        var type = FindType(compiled, className);
        if (type?.GetMethod("TransformText") == null)
        {
            throw new ManiaTemplateCompilationFailedException(
                $"Missing method 'TransformText' in compiled render script for template '{className}'.");
        }

        return type;
    }

    private static Type? FindType(Assembly assembly, string className)
    {
        var type = assembly.GetType(className);
        if (type != null)
        {
            return type;
        }

        // The template class may end up inside the configured namespace, so fall back to a name lookup.
        return assembly.GetTypes().FirstOrDefault(t => t.Name == className);
    }

    private static IReadOnlyList<MetadataReference> BuildReferences(AssemblyLoadContext loadContext,
        IReadOnlyCollection<Assembly> templateAssemblies)
    {
        var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        void Add(Assembly assembly)
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
            {
                return;
            }

            references.TryAdd(assembly.GetName().Name!, GetFileReference(assembly));
        }

        // The first assembly of a name wins, so the assemblies of the template come before other loaded ones, which
        // may contain another version, e.g. of a module that is reloaded while its old version is unloading.

        // The template's assembly and the assemblies it references, e.g. the assemblies of a module and the export
        // assemblies of other modules.
        foreach (var assembly in templateAssemblies)
        {
            Add(assembly);
        }

        // Other assemblies loaded into the load context of the template, e.g. libraries of a module.
        foreach (var assembly in loadContext.Assemblies)
        {
            Add(assembly);
        }

        // The host's assemblies (EvoSC.Common, ManiaTemplates, ...) provide the shared types templates use.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Add(assembly);
        }

        // Assemblies of the framework and the host are only loaded when they are used, so the ones that are not
        // loaded yet are referenced by their files. Loaded assemblies come first, as they are used at runtime.
        foreach (var path in GetPlatformAssemblyPaths())
        {
            references.TryAdd(Path.GetFileNameWithoutExtension(path),
                FileReferenceCache.GetOrAdd(path, static file => MetadataReference.CreateFromFile(file)));
        }

        return references.Values.ToList();
    }

    /// <summary>
    /// Returns the files of the assemblies the default load context resolves, which are the assemblies of the
    /// framework and of the host.
    /// </summary>
    private static string[] GetPlatformAssemblyPaths()
    {
        return (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries) ?? [];
    }

    /// <summary>
    /// Creates the reference to the file of an assembly, cached per load context.
    /// </summary>
    private static MetadataReference GetFileReference(Assembly assembly)
    {
        var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
        var cache = loadContext == null || loadContext == AssemblyLoadContext.Default
            ? FileReferenceCache
            : LoadContextReferenceCache.GetValue(loadContext,
                static _ => new ConcurrentDictionary<string, MetadataReference>());

        return cache.GetOrAdd(assembly.Location, static path => MetadataReference.CreateFromFile(path));
    }
}
