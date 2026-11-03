using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace ManiaTemplates.Tests.IntegrationTests;

/// <summary>
/// Templates may be loaded from collectible load contexts, like the ones of plugins, which have to be collectable
/// again when the context is unloaded.
/// </summary>
public class CollectibleLoadContextTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();

    private const string DataLabelTemplate = """
        <component>
            <using namespace="ManiaTemplates.Tests.IntegrationTests"/>
            <property type="ComplexDataType" name="data"/>
            <template><label text="{{ data.TestString }}" id="{{ data }}"/></template>
        </component>
        """;

    // The component using the type is imported, like a component of another template of the plugin
    private const string SharedTemplate = """
        <component>
            <using namespace="ManiaTemplates.Tests.IntegrationTests"/>
            <import component="DataLabel" as="DataLabel"/>
            <property type="ComplexDataType" name="data"/>
            <template><frame><DataLabel data="{{ data }}"/></frame></template>
        </component>
        """;

    [Fact]
    public void Should_Not_Keep_Unloaded_Load_Context_Of_Template_Alive()
    {
        var loadContext = RenderInLoadContextsAndUnload(unloadTemplateContext: true);

        Assert.True(IsCollected(loadContext), "The unloaded load context is still referenced.");
        Assert.Throws<KeyNotFoundException>(() => _maniaTemplateEngine.GetComponent("Shared"));
    }

    [Fact]
    public void Should_Not_Keep_Unloaded_Load_Context_Of_Referenced_Assembly_Alive()
    {
        var loadContext = RenderInLoadContextsAndUnload(unloadTemplateContext: false);

        Assert.True(IsCollected(loadContext), "The unloaded load context is still referenced.");
        Assert.Throws<KeyNotFoundException>(() => _maniaTemplateEngine.GetComponent("Shared"));
    }

    [Fact]
    public async Task Should_Compile_Template_Into_Load_Context_Of_Its_Assembly()
    {
        var loadContext = new AssemblyLoadContext("CollectibleLoadContextTest", isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(typeof(ComplexDataType).Assembly.Location);
            _maniaTemplateEngine.AddTemplateFromString("DataLabel", DataLabelTemplate, assembly);
            _maniaTemplateEngine.AddTemplateFromString("Shared", SharedTemplate, assembly);

            var result = await _maniaTemplateEngine.RenderAsync("Shared",
                new Dictionary<string, object?> { ["data"] = CreateData(assembly, "collectible") });

            Assert.Contains("<label text=\"collectible\"", result);
            Assert.Contains(loadContext.Assemblies,
                loaded => loaded.GetName().Name!.StartsWith("ManiaTemplates.Templates.MtShared."));
        }
        finally
        {
            loadContext.Unload();
        }
    }

    /// <summary>
    /// Renders a template of a collectible load context, which references an assembly of another collectible context.
    /// Unloads one of the contexts and returns a weak reference to it. Not inlined, so no local variable keeps the
    /// contexts alive.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference RenderInLoadContextsAndUnload(bool unloadTemplateContext)
    {
        var templateContext = new AssemblyLoadContext("CollectibleLoadContextTest.Template", isCollectible: true);
        var referencedContext = new AssemblyLoadContext("CollectibleLoadContextTest.Referenced", isCollectible: true);
        var assembly = templateContext.LoadFromAssemblyPath(typeof(ComplexDataType).Assembly.Location);
        var referencedAssembly = referencedContext.LoadFromAssemblyPath(typeof(ComplexDataType).Assembly.Location);

        _maniaTemplateEngine.AddTemplateFromString("DataLabel", DataLabelTemplate, assembly);
        _maniaTemplateEngine.AddTemplateFromString("Shared", SharedTemplate, assembly, referencedAssembly);

        var result = _maniaTemplateEngine
            .RenderAsync("Shared", new Dictionary<string, object?> { ["data"] = CreateData(assembly, "collectible") })
            .GetAwaiter().GetResult();
        Assert.Contains("<label text=\"collectible\"", result);

        var unloaded = unloadTemplateContext ? templateContext : referencedContext;
        unloaded.Unload();
        return new WeakReference(unloaded);
    }

    private static object CreateData(Assembly assembly, string text)
    {
        var dataType = assembly.GetType(typeof(ComplexDataType).FullName!)!;
        var data = Activator.CreateInstance(dataType)!;
        dataType.GetProperty(nameof(ComplexDataType.TestString))!.SetValue(data, text);
        return data;
    }

    private static bool IsCollected(WeakReference reference)
    {
        // Unloading takes multiple garbage collections
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (reference.IsAlive && DateTime.UtcNow < deadline)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return !reference.IsAlive;
    }
}
