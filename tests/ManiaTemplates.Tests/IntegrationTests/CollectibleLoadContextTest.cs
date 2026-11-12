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

    [Fact]
    public async Task Should_Keep_Host_Template_When_Module_Importing_It_Unloads()
    {
        _maniaTemplateEngine.AddTemplateFromString("HostLabel",
            """<component><property type="string" name="text"/><template><label text="{{ text }}"/></template></component>""");
        Assert.Contains("<label text=\"host\"", await _maniaTemplateEngine.RenderAsync("HostLabel", new { text = "host" }));

        var loadContext = RenderModuleImportingHostTemplateAndUnload();

        Assert.True(IsCollected(loadContext), "The unloaded load context is still referenced.");
        Assert.Throws<KeyNotFoundException>(() => _maniaTemplateEngine.GetComponent("ModulePanel"));

        // The host template is not compiled again, as it does not depend on the module
        Assert.Contains("<label text=\"host\"", await _maniaTemplateEngine.RenderAsync("HostLabel", new { text = "host" }));
        Assert.Single(AppDomain.CurrentDomain.GetAssemblies(),
            assembly => assembly.GetName().Name!.StartsWith("ManiaTemplates.Templates.MtHostLabel."));
    }

    [Fact]
    public async Task Should_Collect_Module_Imported_By_Template_Of_Another_Module()
    {
        var (unloaded, importingContext) = RenderModuleImportingOtherModuleAndUnloadOther();
        try
        {
            Assert.True(IsCollected(unloaded), "The unloaded load context is still referenced.");

            // The importing template is kept, but can't be compiled without the removed component
            Assert.NotNull(_maniaTemplateEngine.GetComponent("BModule.Panel"));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _maniaTemplateEngine.RenderAsync("BModule.Panel", new { }));
        }
        finally
        {
            importingContext.Unload();
        }
    }

    [Fact]
    public async Task Should_Render_Template_Of_Reloaded_Module()
    {
        var unloaded = RenderModuleAndUnload("first");
        Assert.True(IsCollected(unloaded), "The unloaded load context is still referenced.");

        var (loadContext, assembly) = LoadModule("CollectibleLoadContextTest.Reloaded");
        try
        {
            _maniaTemplateEngine.AddTemplateFromString("Module.Label", DataLabelTemplate, assembly);

            var result = await _maniaTemplateEngine.RenderAsync("Module.Label",
                new Dictionary<string, object?> { ["data"] = CreateData(assembly, "second") });

            Assert.Contains("<label text=\"second\"", result);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public async Task Should_Load_Embedded_Template_Into_Load_Context_Of_Its_Assembly()
    {
        var (loadContext, assembly) = LoadModule("CollectibleLoadContextTest.Embedded");
        try
        {
            // Called through the copy of the test assembly in the collectible load context
            assembly.GetType(typeof(EmbeddedTemplates).FullName!)!
                .GetMethod(nameof(EmbeddedTemplates.Load))!
                .Invoke(null, [_maniaTemplateEngine]);

            var result = await _maniaTemplateEngine.RenderAsync(EmbeddedTemplates.LabelResource,
                new Dictionary<string, object?> { ["data"] = CreateData(assembly, "embedded") });

            Assert.Contains("<label text=\"embedded\"", result);
            Assert.Contains(loadContext.Assemblies,
                loaded => loaded.GetName().Name!.StartsWith("ManiaTemplates.Templates."));
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference RenderModuleImportingHostTemplateAndUnload()
    {
        var (loadContext, assembly) = LoadModule("CollectibleLoadContextTest.Module");
        _maniaTemplateEngine.AddTemplateFromString("ModulePanel", """
            <component>
                <using namespace="ManiaTemplates.Tests.IntegrationTests"/>
                <import component="HostLabel" as="HostLabel"/>
                <property type="ComplexDataType" name="data"/>
                <template><frame><HostLabel text="{{ data.TestString }}"/></frame></template>
            </component>
            """, assembly);

        var result = _maniaTemplateEngine
            .RenderAsync("ModulePanel", new Dictionary<string, object?> { ["data"] = CreateData(assembly, "module") })
            .GetAwaiter().GetResult();
        Assert.Contains("<label text=\"module\"", result);

        loadContext.Unload();
        return new WeakReference(loadContext);
    }

    /// <summary>
    /// Renders a template of one module importing a component of another module, then unloads the other module.
    /// Returns a weak reference to the unloaded context and the context of the importing module.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private (WeakReference Unloaded, AssemblyLoadContext Importing) RenderModuleImportingOtherModuleAndUnloadOther()
    {
        var (importedContext, importedAssembly) = LoadModule("CollectibleLoadContextTest.AModule");
        var (importingContext, importingAssembly) = LoadModule("CollectibleLoadContextTest.BModule");

        _maniaTemplateEngine.AddTemplateFromString("AModule.Label",
            """<component><property type="string" name="text"/><template><label text="{{ text }}"/></template></component>""",
            importedAssembly);
        _maniaTemplateEngine.AddTemplateFromString("BModule.Panel", """
            <component>
                <import component="AModule.Label" as="ALabel"/>
                <template><frame><ALabel text="imported"/></frame></template>
            </component>
            """, importingAssembly);

        var result = _maniaTemplateEngine.RenderAsync("BModule.Panel", new { }).GetAwaiter().GetResult();
        Assert.Contains("<label text=\"imported\"", result);

        importedContext.Unload();
        return (new WeakReference(importedContext), importingContext);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference RenderModuleAndUnload(string text)
    {
        var (loadContext, assembly) = LoadModule("CollectibleLoadContextTest.First");
        _maniaTemplateEngine.AddTemplateFromString("Module.Label", DataLabelTemplate, assembly);

        var result = _maniaTemplateEngine
            .RenderAsync("Module.Label", new Dictionary<string, object?> { ["data"] = CreateData(assembly, text) })
            .GetAwaiter().GetResult();
        Assert.Contains($"<label text=\"{text}\"", result);

        loadContext.Unload();
        return new WeakReference(loadContext);
    }

    /// <summary>
    /// Loads a copy of the test assembly into a new collectible load context, like a module.
    /// </summary>
    private static (AssemblyLoadContext LoadContext, Assembly Assembly) LoadModule(string name)
    {
        var loadContext = new AssemblyLoadContext(name, isCollectible: true);
        return (loadContext, loadContext.LoadFromAssemblyPath(typeof(ComplexDataType).Assembly.Location));
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
