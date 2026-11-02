using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Tests.Lib;

public class MtScriptTransformerTest
{
    private readonly MtScriptTransformer _scriptTransformer = new(new MtLanguageT4());

    [Fact]
    public void Should_Not_Create_Directives_Block_Without_Directives()
    {
        _scriptTransformer.CreateManiaScriptBlock(ComponentWithScripts("Void Fn() {}"));

        Assert.Equal("", _scriptTransformer.CreateManiaScriptDirectivesBlock());
    }

    [Fact]
    public void Should_Move_Directives_To_Directives_Block()
    {
        var scriptBlock = _scriptTransformer.CreateManiaScriptBlock(ComponentWithScripts("""
            #Include "TextLib" as TextLib
            #Const C_Size 10
            #Struct K_Point { Integer X; Integer Y; }
            Void Fn() {}
            """));

        Assert.DoesNotContain("#Include", scriptBlock);
        Assert.DoesNotContain("#Const", scriptBlock);
        Assert.DoesNotContain("#Struct", scriptBlock);
        Assert.Contains("Void Fn() {}", scriptBlock);

        Assert.Equal("""
            <script><!--
            #Include "TextLib" as TextLib
            #Struct K_Point { Integer X; Integer Y; }
            #Const C_Size 10
            --></script>

            """, _scriptTransformer.CreateManiaScriptDirectivesBlock(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Merge_Directives_Of_Multiple_Scripts()
    {
        _scriptTransformer.CreateManiaScriptBlock(ComponentWithScripts(
            """
            #Include "TextLib" as TextLib
            #Const C_Size 10
            #Struct K_Point { Integer X; Integer Y; }
            """,
            """
            #Include "TextLib" as TextLib
            #Include "MathLib" as MathLib
            #Const C_Size 10
            #Struct K_Point {Integer X;Integer Y;}
            """
        ));

        Assert.Equal("""
            <script><!--
            #Include "TextLib" as TextLib
            #Include "MathLib" as MathLib
            #Struct K_Point { Integer X; Integer Y; }
            #Const C_Size 10
            --></script>

            """, _scriptTransformer.CreateManiaScriptDirectivesBlock(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Throw_Exception_For_Conflicting_Include_Alias()
    {
        var component = ComponentWithScripts(
            """#Include "TextLib" as Lib""",
            """#Include "MathLib" as Lib"""
        );

        Assert.Throws<DuplicateManiaScriptIncludeException>(() => _scriptTransformer.CreateManiaScriptBlock(component));
    }

    [Fact]
    public void Should_Throw_Exception_For_Redefined_Constant()
    {
        var component = ComponentWithScripts(
            "#Const C_Size 10",
            "#Const  C_Size   10",
            "#Const C_Size 20"
        );

        Assert.Throws<DuplicateManiaScriptConstantException>(() => _scriptTransformer.CreateManiaScriptBlock(component));
    }

    [Fact]
    public void Should_Throw_Exception_For_Multiple_Main_Methods()
    {
        var component = new MtComponent
        {
            TemplateContent = "",
            HasSlot = false,
            ImportedComponents = new MtComponentMap(),
            Properties = new(),
            Namespaces = new(),
            Scripts = new()
            {
                new() { Content = "main() { log(1); }", HasMainMethod = true, Once = false },
                new() { Content = "main() { log(2); }", HasMainMethod = true, Once = false }
            }
        };

        Assert.Throws<DuplicateMainManiaScriptException>(() => _scriptTransformer.CreateManiaScriptBlock(component));
    }

    [Fact]
    public void Should_Allow_Same_Main_Method_Multiple_Times()
    {
        var component = new MtComponent
        {
            TemplateContent = "",
            HasSlot = false,
            ImportedComponents = new MtComponentMap(),
            Properties = new(),
            Namespaces = new(),
            Scripts = new() { new() { Content = "main() { log(1); }", HasMainMethod = true, Once = true } }
        };

        _scriptTransformer.CreateManiaScriptBlock(component);
        _scriptTransformer.CreateManiaScriptBlock(component);
    }

    [Fact]
    public void Should_Throw_Exception_For_Redefined_Struct()
    {
        var component = ComponentWithScripts(
            "#Struct K_Point { Integer X; Integer Y; }",
            "#Struct K_Point { Real X; Real Y; }"
        );

        Assert.Throws<DuplicateManiaScriptStructException>(() => _scriptTransformer.CreateManiaScriptBlock(component));
    }

    [Fact]
    public async Task Should_Render_Directives_Of_Nested_Components_Once()
    {
        var engine = new ManiaTemplateEngine();
        engine.AddTemplateFromString("ScriptChild", """
            <component>
                <template><label/></template>
                <script><!--
                #Include "TextLib" as TextLib
                #Const C_Size 10
                Void ChildFn() {}
                --></script>
            </component>
            """);
        engine.AddTemplateFromString("ScriptParent", """
            <component>
                <import component="ScriptChild" as="ScriptChild"/>
                <template><ScriptChild/><ScriptChild/></template>
                <script><!--
                #Include "TextLib" as TextLib
                main() { log(C_Size); }
                --></script>
            </component>
            """);

        var result = await engine.RenderAsync("ScriptParent", new { }, new[] { typeof(ManiaTemplateEngine).Assembly });

        Assert.Equal(1, CountOccurrences(result, "#Include \"TextLib\" as TextLib"));
        Assert.Equal(1, CountOccurrences(result, "#Const C_Size 10"));
        Assert.Equal(2, CountOccurrences(result, "Void ChildFn() {}"));
        Assert.True(result.IndexOf("#Include", StringComparison.Ordinal) <
                    result.IndexOf("<label", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Insert_Raw_And_Escaped_Values_Into_ManiaScript()
    {
        var engine = new ManiaTemplateEngine();
        engine.AddTemplateFromString("ScriptOutput", """
            <component>
                <property type="string" name="value"/>
                <template><label/></template>
                <script><!--
                main() { log("{{ value }}"); log("{! value !}"); }
                --></script>
            </component>
            """);

        var result = await engine.RenderAsync("ScriptOutput", new { value = "<b>" },
            new[] { typeof(ManiaTemplateEngine).Assembly });

        Assert.Contains("main() { log(\"&lt;b&gt;\"); log(\"<b>\"); }", result);
    }

    private static MtComponent ComponentWithScripts(params string[] scripts) => new()
    {
        TemplateContent = "",
        HasSlot = false,
        ImportedComponents = new MtComponentMap(),
        Properties = new(),
        Namespaces = new(),
        Scripts = scripts.Select(script => new MtComponentScript
        {
            Content = script,
            HasMainMethod = false,
            Once = false
        }).ToList()
    };

    private static int CountOccurrences(string haystack, string needle) =>
        haystack.Split(needle).Length - 1;
}
