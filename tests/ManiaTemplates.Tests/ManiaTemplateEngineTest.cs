using System.Diagnostics;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests;

public class ManiaTemplateEngineTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();

    private const string LabelTemplate = """
        <component>
            <property type="string" name="text"/>
            <template><label text="{{ text }}"/></template>
        </component>
        """;

    [Fact]
    public async Task Should_Render_Dictionary_Data()
    {
        _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);

        var result = await _maniaTemplateEngine.RenderAsync("Label",
            new Dictionary<string, object?> { { "text", "unit" } });

        Assert.Equal("""
            <manialink version="3" id="MtLabel" name="EvoSC#-MtLabel">
            <label text="unit" />
            </manialink>

            """, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task Should_Skip_Global_Variables_Without_Value()
    {
        _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);
        _maniaTemplateEngine.GlobalVariables["empty"] = null;

        var result = await _maniaTemplateEngine.RenderAsync("Label", new { text = "unit" });

        Assert.Contains("<label text=\"unit\" />", result);
    }

    [Fact]
    public async Task Should_Throw_Exception_For_Invalid_Expressions()
    {
        _maniaTemplateEngine.AddTemplateFromString("Broken",
            "<component><template><label text=\"{{ undefinedVariable }}\"/></template></component>");

        await Assert.ThrowsAsync<ManiaTemplateCompilationFailedException>(() =>
            _maniaTemplateEngine.RenderAsync("Broken", new { }));
    }

    [Fact]
    public async Task Should_Use_Framework_Types_Of_Assemblies_That_Are_Not_Loaded()
    {
        // The test host loads all framework assemblies, so the template is rendered by a plain process
        var host = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            ArgumentList =
            {
                Path.Combine(AppContext.BaseDirectory, "ManiaTemplates.Host.dll"),
                "<component><template><label text=\"{{ System.Formats.Tar.TarEntryType.Directory }}\"/></template></component>"
            },
            RedirectStandardOutput = true
        };

        using var process = Process.Start(host)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Contains("<label text=\"Directory\" />", output);
    }

    [Fact]
    public async Task Should_Keep_Default_Attributes_In_ManiaScript()
    {
        _maniaTemplateEngine.AddTemplateFromString("Script", """
            <component>
                <template><label pos="0 0" text="unit"/></template>
                <script><!--
                main() {
                    declare Text Empty="";
                    declare Text Label = "<label pos="0 0" halign="left"/>";
                }
                --></script>
            </component>
            """);

        var result = await _maniaTemplateEngine.RenderAsync("Script", new { });

        Assert.Contains("<label text=\"unit\" />", result);
        Assert.Contains("declare Text Empty=\"\";", result);
        Assert.Contains("declare Text Label = \"<label pos=\"0 0\" halign=\"left\"/>\";", result);
    }

    [Fact]
    public async Task Should_Throw_Exception_For_Missing_Component_Root()
    {
        _maniaTemplateEngine.AddTemplateFromString("NoRoot", "<template><label/></template>");

        await Assert.ThrowsAsync<MissingComponentRootException>(() =>
            _maniaTemplateEngine.RenderAsync("NoRoot", new { }));
    }

    [Fact]
    public async Task Should_Hide_Rendered_Template()
    {
        _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);
        var rendered = await _maniaTemplateEngine.RenderAsync("Label", new { text = "unit" });

        Assert.Contains("id=\"MtLabel\"", rendered);
        Assert.Equal("<manialink id=\"MtLabel\"></manialink>", _maniaTemplateEngine.Hide("Label"));
    }

    [Fact]
    public async Task Should_Return_Failed_Task_When_Pre_Processing_Fails()
    {
        _maniaTemplateEngine.AddTemplateFromString("Broken",
            "<component><template><label text=\"{{ undefinedVariable }}\"/></template></component>");

        var preProcessing = _maniaTemplateEngine.PreProcessAsync("Broken");

        await Assert.ThrowsAsync<ManiaTemplateCompilationFailedException>(() => preProcessing);
    }

    [Fact]
    public async Task Should_Write_T4_Template_When_Pre_Processing()
    {
        var directory = Directory.CreateTempSubdirectory("ManiaTemplatesTest");
        try
        {
            _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);
            await _maniaTemplateEngine.PreProcessAsync("Label", directory.FullName);

            var t4Template = File.ReadAllText(Path.Combine(directory.FullName, "MtLabel.tt"));
            Assert.Contains("<label text=", t4Template);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task Should_Render_Replaced_Template_After_Removal()
    {
        _maniaTemplateEngine.AddTemplateFromString("Replaceable",
            "<component><template><label/></template></component>");
        await _maniaTemplateEngine.RenderAsync("Replaceable", new { });

        _maniaTemplateEngine.RemoveTemplate("Replaceable");
        _maniaTemplateEngine.AddTemplateFromString("Replaceable",
            "<component><template><quad/></template></component>");
        var result = await _maniaTemplateEngine.RenderAsync("Replaceable", new { });

        Assert.Contains("<quad />", result);
        Assert.DoesNotContain("<label", result);
    }

    [Fact]
    public void Should_Throw_Exception_When_Removing_Unknown_Template()
    {
        Assert.Throws<InvalidOperationException>(() => _maniaTemplateEngine.RemoveTemplate("Unknown"));
    }

    [Fact]
    public void Should_Add_Get_And_Remove_ManiaScripts()
    {
        _maniaTemplateEngine.AddManiaScriptFromString("script", "main() {}");
        Assert.Equal("main() {}", _maniaTemplateEngine.GetManiaScript("script"));

        _maniaTemplateEngine.RemoveManiaScript("script");
        Assert.Throws<ManiaScriptNotFoundException>(() => _maniaTemplateEngine.GetManiaScript("script"));
    }

    [Fact]
    public void Should_Throw_Exception_For_Duplicate_ManiaScript()
    {
        _maniaTemplateEngine.AddManiaScriptFromString("script", "main() {}");

        Assert.Throws<ManiaScriptAlreadyExistsException>(() =>
            _maniaTemplateEngine.AddManiaScriptFromString("script", "main() {}"));
    }

    [Fact]
    public void Should_Throw_Exception_When_Removing_Unknown_ManiaScript()
    {
        Assert.Throws<InvalidOperationException>(() => _maniaTemplateEngine.RemoveManiaScript("Unknown"));
    }

    [Fact]
    public void Should_List_Components_Without_Properties_In_Markdown()
    {
        _maniaTemplateEngine.AddTemplateFromString("Plain", "<component><template><label/></template></component>");

        var markdown = new MtComponentMarkdownGenerator
        {
            Engine = _maniaTemplateEngine,
            Components = new MtComponentMap { { "Plain", new MtComponentImport { TemplateKey = "Plain", Tag = "Plain" } } }
        }.Generate();

        Assert.Equal("""
            # Component list

            This list contains available components and their attributes.

            ## Plain

            `<Plain />`

            """, markdown, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Components_Markdown_Should_Be_Up_To_Date()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ManiaTemplates.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var componentsMarkdown = File.ReadAllText(Path.Combine(directory.FullName, "components.md"));

        Assert.Equal(componentsMarkdown, _maniaTemplateEngine.GenerateComponentsMarkdown(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task Should_Pre_Process_And_Render_Templates_In_Parallel()
    {
        _maniaTemplateEngine.AddTemplateFromString("Shared", LabelTemplate);
        var keys = Enumerable.Range(0, 20).Select(i => $"Parallel{i}").ToList();
        foreach (var key in keys)
        {
            _maniaTemplateEngine.AddTemplateFromString(key,
                $"<component><import component=\"Shared\" as=\"Shared\"/><template><Shared text=\"{key}\"/></template></component>");
        }

        await Task.WhenAll(keys.Select(key => _maniaTemplateEngine.PreProcessAsync(key)));
        var results = await Task.WhenAll(keys.Select(key =>
            _maniaTemplateEngine.RenderAsync(key, new Dictionary<string, object?>())));

        for (var i = 0; i < keys.Count; i++)
        {
            Assert.Contains($"<label text=\"{keys[i]}\" />", results[i]);
        }
    }


    [Fact]
    public async Task Should_Compile_Failed_Template_Again()
    {
        _maniaTemplateEngine.AddTemplateFromString("NeedsGlobal",
            "<component><template><label text=\"{{ serverName }}\"/></template></component>");

        await Assert.ThrowsAsync<ManiaTemplateCompilationFailedException>(() =>
            _maniaTemplateEngine.RenderAsync("NeedsGlobal", new { }));

        _maniaTemplateEngine.GlobalVariables["serverName"] = "unit";
        Assert.Contains("<label text=\"unit\" />",
            await _maniaTemplateEngine.RenderAsync("NeedsGlobal", new { }));
    }

    [Fact]
    public async Task Should_Cancel_Pre_Processing()
    {
        _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _maniaTemplateEngine.PreProcessAsync("Label", new CancellationToken(true)));

        Assert.Contains("<label text=\"unit\" />",
            await _maniaTemplateEngine.RenderAsync("Label", new { text = "unit" }));
    }

    [Fact]
    public async Task Should_Cancel_Rendering()
    {
        _maniaTemplateEngine.AddTemplateFromString("Label", LabelTemplate);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _maniaTemplateEngine.RenderAsync("Label", new { text = "unit" }, new CancellationToken(true)));
    }


    [Fact]
    public async Task Should_Not_Cache_Template_Replaced_While_Pre_Processing()
    {
        await ReplaceTemplateDuringCompilation(() => _maniaTemplateEngine.PreProcessAsync("Replaced"));

        var result = await _maniaTemplateEngine.RenderAsync("Replaced", new { });
        Assert.Contains("<quad />", result);
        Assert.DoesNotContain("<label", result);
    }

    [Fact]
    public async Task Should_Not_Cache_Template_Replaced_While_Rendering()
    {
        await ReplaceTemplateDuringCompilation(() => _maniaTemplateEngine.RenderAsync("Replaced", new { }));

        var result = await _maniaTemplateEngine.RenderAsync("Replaced", new { });
        Assert.Contains("<quad />", result);
        Assert.DoesNotContain("<label", result);
    }

    /// <summary>
    /// Replaces the template while it is compiled. The compiler reads the location of the template's assemblies after
    /// the template was read, so blocking it pauses the compilation.
    /// </summary>
    private async Task ReplaceTemplateDuringCompilation(Func<Task> compile)
    {
        var compilationStarted = new TaskCompletionSource();
        using var continueCompilation = new ManualResetEventSlim();
        var blockingAssembly = new BlockingAssembly(typeof(ManiaTemplateEngine).Assembly, () =>
        {
            compilationStarted.TrySetResult();
            continueCompilation.Wait();
        });

        _maniaTemplateEngine.AddTemplateFromString("Replaced", "<component><template><label/></template></component>",
            typeof(ManiaTemplateEngine).Assembly, blockingAssembly);
        var compilation = compile();

        await compilationStarted.Task;
        _maniaTemplateEngine.RemoveTemplate("Replaced");
        _maniaTemplateEngine.AddTemplateFromString("Replaced", "<component><template><quad/></template></component>");
        continueCompilation.Set();
        await compilation;
    }

    /// <summary>
    /// An assembly calling the given action when its location is read.
    /// </summary>
    private sealed class BlockingAssembly(System.Reflection.Assembly assembly, Action onLocation)
        : System.Reflection.Assembly
    {
        public override string Location
        {
            get
            {
                onLocation();
                return assembly.Location;
            }
        }

        public override bool IsDynamic => false;

        public override string? FullName => assembly.FullName;

        public override System.Reflection.AssemblyName GetName() => assembly.GetName();
    }

    [Fact]
    public async Task Should_Compile_Template_Again_When_Imported_Template_Is_Replaced()
    {
        _maniaTemplateEngine.AddTemplateFromString("Inner", "<component><template><label/></template></component>");
        _maniaTemplateEngine.AddTemplateFromString("Outer",
            "<component><import component=\"Inner\" as=\"Inner\"/><template><frame><Inner/></frame></template></component>");
        Assert.Contains("<label />", await _maniaTemplateEngine.RenderAsync("Outer", new { }));

        _maniaTemplateEngine.RemoveTemplate("Inner");
        _maniaTemplateEngine.AddTemplateFromString("Inner", "<component><template><quad/></template></component>");

        var result = await _maniaTemplateEngine.RenderAsync("Outer", new { });
        Assert.Contains("<quad />", result);
        Assert.DoesNotContain("<label", result);
    }
}
