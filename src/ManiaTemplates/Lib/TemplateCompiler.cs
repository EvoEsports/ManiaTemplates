using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using ManiaTemplates.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ManiaTemplates.Lib;

/// <summary>
/// Compiles pre-processed ManiaTemplates template classes into assemblies that live in the same
/// <see cref="AssemblyLoadContext"/> as the assemblies the template is rendered against.
/// </summary>
/// <remarks>
/// A template references the types it renders with (module settings, models, ...). Those types may live in a
/// collectible load context. Emitting the compiled template into that same context keeps the type identities
/// intact and lets both the template and the assemblies it binds to be unloaded together.
/// </remarks>
internal static class TemplateCompiler
{
    private static int _assemblyCounter;

    // Mirrors the implicit usings of the previous Roslyn scripting based compilation.
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

    private static readonly ConcurrentDictionary<string, MetadataReference> FileReferenceCache = new();

    private static readonly ConcurrentDictionary<AssemblyLoadContext, IReadOnlyList<MetadataReference>>
        ReferenceCache = new();

    /// <summary>
    /// Compiles the given pre-compiled template and returns the type declared by <paramref name="className"/>.
    /// </summary>
    public static Type Compile(string className, string preCompiledTemplate, IEnumerable<Assembly> assemblies)
    {
        var assemblyList = assemblies as IReadOnlyCollection<Assembly> ?? assemblies.ToList();
        var loadContext = ResolveLoadContext(assemblyList);

        var syntaxTree = CSharpSyntaxTree.ParseText(preCompiledTemplate,
            new CSharpParseOptions(LanguageVersion.Latest));

        var compilation = CSharpCompilation.Create(
            $"ManiaTemplates.Templates.{className}.{Interlocked.Increment(ref _assemblyCounter)}",
            [syntaxTree],
            GetReferences(loadContext, assemblyList),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true,
                usings: DefaultUsings));

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);

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
            // Loading into the same context the template's assemblies were loaded into is what keeps the types
            // the template binds to identical to the ones the caller passed in.
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

    /// <summary>
    /// Picks the load context the template should be emitted into, preferring a non-default context so
    /// collectible module assemblies can be referenced without being duplicated.
    /// </summary>
    private static AssemblyLoadContext ResolveLoadContext(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var context = AssemblyLoadContext.GetLoadContext(assembly);
            if (context != null && context != AssemblyLoadContext.Default)
            {
                return context;
            }
        }

        return AssemblyLoadContext.Default;
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

    private static IReadOnlyList<MetadataReference> GetReferences(AssemblyLoadContext loadContext,
        IReadOnlyCollection<Assembly> templateAssemblies)
    {
        return ReferenceCache.GetOrAdd(loadContext, context => BuildReferences(context, templateAssemblies));
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

            references.TryAdd(assembly.GetName().Name!, GetFileReference(assembly.Location));
        }

        // The host's assemblies (EvoSC.Common, ManiaTemplates, ...) provide the shared types templates use.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Add(assembly);
        }

        // Types from the render context, e.g. a collectible module's settings interfaces.
        foreach (var assembly in templateAssemblies)
        {
            Add(assembly);
        }

        // Types shared inside the load context of the template, e.g. exported module contracts.
        foreach (var assembly in loadContext.Assemblies)
        {
            Add(assembly);
        }

        return references.Values.ToList();
    }

    private static MetadataReference GetFileReference(string path)
    {
        return FileReferenceCache.GetOrAdd(path, static p => MetadataReference.CreateFromFile(p));
    }
}
