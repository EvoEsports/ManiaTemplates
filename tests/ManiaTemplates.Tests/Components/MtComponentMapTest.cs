using ManiaTemplates.Components;

namespace ManiaTemplates.Tests.Components;

public class MtComponentMapTest
{
    [Fact]
    public void Should_Overload_Entries()
    {
        var original = new MtComponentImport { Tag = "test", TemplateKey = "original" };
        var kept = new MtComponentImport { Tag = "kept", TemplateKey = "kept" };
        var replacement = new MtComponentImport { Tag = "test", TemplateKey = "replacement" };
        var added = new MtComponentImport { Tag = "added", TemplateKey = "added" };
        var map = new MtComponentMap { { "test", original }, { "kept", kept } };

        var result = map.Overload(new Dictionary<string, MtComponentImport>
        {
            { "test", replacement },
            { "added", added }
        });

        Assert.Equal(3, result.Count);
        Assert.Same(replacement, result["test"]);
        Assert.Same(kept, result["kept"]);
        Assert.Same(added, result["added"]);
        Assert.Same(original, map["test"]);
        Assert.Equal(2, map.Count);
    }
}
