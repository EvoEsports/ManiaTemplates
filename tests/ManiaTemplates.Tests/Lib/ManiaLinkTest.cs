using System.Dynamic;
using System.Runtime.Loader;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Lib;
using ManiaTemplates.Tests.IntegrationTests;

namespace ManiaTemplates.Tests.Lib;

public class ManiaLinkTest
{
    private const string TemplateSource = """
        public class MtTest
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
            public string Global { get; set; } = "";

            public string TransformText() =>
                $"<label text=\"{Name}\" count=\"{Count}\" global=\"{Global}\" pos=\"0 0\" opacity=\"1\" halign=\"left\"/>";
        }
        """;

    private readonly ManiaLink _maniaLink = new("MtTest", TemplateSource, new[] { typeof(ManiaLink).Assembly });

    [Fact]
    public async Task Should_Render_Object_Data()
    {
        var result = await _maniaLink.RenderAsync(new { Name = "unit", Count = 3, Unknown = "ignored" });

        Assert.Equal("<label text=\"unit\" count=\"3\"/>", result);
    }

    [Fact]
    public async Task Should_Render_Dictionary_Data()
    {
        var result = await _maniaLink.RenderAsync(new Dictionary<string, object?>
        {
            { "Name", "unit" },
            { "Count", 3 },
            { "Unknown", "ignored" }
        });

        Assert.Equal("<label text=\"unit\" count=\"3\"/>", result);
    }

    [Fact]
    public async Task Should_Render_Expando_Data()
    {
        dynamic data = new ExpandoObject();
        data.Name = "unit";
        data.Count = 3;

        var result = await _maniaLink.RenderAsync((ExpandoObject)data);

        Assert.Equal("<label text=\"unit\" count=\"3\"/>", result);
    }

    [Fact]
    public async Task Should_Pass_Global_Variables()
    {
        var globals = new Dictionary<string, object?> { { "Global", "gvar" }, { "Unknown", "ignored" } };

        Assert.Equal("<label text=\"unit\" count=\"0\" global=\"gvar\"/>",
            await _maniaLink.RenderAsync(new { Name = "unit" }, globals));
        Assert.Equal("<label text=\"unit\" count=\"0\" global=\"gvar\"/>",
            await _maniaLink.RenderAsync(new Dictionary<string, object?> { { "Name", "unit" } }, globals));
    }

    [Fact]
    public void Should_Create_Empty_Manialink_To_Hide()
    {
        Assert.Equal("<manialink id=\"MtTest\"></manialink>", _maniaLink.Hide());
    }

    [Theory]
    [InlineData(3, null, "<manialink version=\"3\" id=\"MtTest\" name=\"EvoSC#-MtTest\">")]
    [InlineData(2, "ScoresTable", "<manialink version=\"2\" id=\"MtTest\" name=\"EvoSC#-MtTest\" layer=\"ScoresTable\">")]
    public void Should_Create_Manialink_Tags(int version, string? layer, string expected)
    {
        Assert.Equal(expected, ManiaLink.OpenTag("MtTest", version, layer));
        Assert.Equal("</manialink>", ManiaLink.CloseTag());
    }

    [Fact]
    public async Task Should_Find_Template_Class_Inside_Namespace()
    {
        var maniaLink = new ManiaLink("MtNamespaced",
            "namespace Some.Namespace { public class MtNamespaced { public string TransformText() => \"<frame/>\"; } }",
            new[] { typeof(ManiaLink).Assembly });

        Assert.Equal("<frame/>", await maniaLink.RenderAsync(new { }));
    }

    [Fact]
    public void Should_Throw_Exception_For_Invalid_Template_Source()
    {
        var exception = Assert.Throws<ManiaTemplateCompilationFailedException>(() =>
            new ManiaLink("MtBroken", "public class MtBroken { public string TransformText() => unknown; }",
                new[] { typeof(ManiaLink).Assembly }));

        Assert.Contains("unknown", exception.Message);
    }

    [Theory]
    [InlineData("public class MtNoTransform { }")]
    [InlineData("public class MtOther { public string TransformText() => \"\"; }")]
    public void Should_Throw_Exception_For_Missing_Render_Method(string source)
    {
        Assert.Throws<ManiaTemplateCompilationFailedException>(() =>
            new ManiaLink("MtNoTransform", source, new[] { typeof(ManiaLink).Assembly }));
    }

    [Fact]
    public async Task Should_Compile_Into_Load_Context_Of_Collectible_Assemblies()
    {
        var loadContext = new AssemblyLoadContext("ManiaLinkTest", isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(typeof(ComplexDataType).Assembly.Location);
            var complexDataType = assembly.GetType(typeof(ComplexDataType).FullName!)!;

            var maniaLink = new ManiaLink("MtCollectible", $$"""
                public class MtCollectible
                {
                    public {{typeof(ComplexDataType).FullName}}? Data { get; set; }
                    public string TransformText() => $"<label text=\"{Data?.TestString}\"/>";
                }
                """, new[] { assembly });

            var data = Activator.CreateInstance(complexDataType)!;
            complexDataType.GetProperty(nameof(ComplexDataType.TestString))!.SetValue(data, "collectible");

            Assert.Equal("<label text=\"collectible\"/>", await maniaLink.RenderAsync(new { Data = data }));
            Assert.Contains(loadContext.Assemblies, a => a.GetName().Name!.StartsWith("ManiaTemplates.Templates.MtCollectible"));
        }
        finally
        {
            loadContext.Unload();
        }
    }
}
