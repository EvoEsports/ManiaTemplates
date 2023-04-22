using ManiaTemplates.Components;

namespace ManiaTemplates.Tests.Components;

public class MtComponentMapTest
{
    private readonly MtComponentMap _mtComponentMap = new();

    public MtComponentMapTest()
    {
        _mtComponentMap["test"] = new MtComponentImport { Tag = "preTest", TemplateKey = "preTest" };
        Assert.Single(_mtComponentMap);
    }

    [Fact]
    public void ShouldOverloadEntries()
    {
        var test = new MtComponentImport { Tag = "test", TemplateKey = "test" };
        var entry = new MtComponentImport { Tag = "entry", TemplateKey = "entry" };
        var overload = new Dictionary<string, MtComponentImport>
        {
            { "test", test },
            { "entry", entry }
        };

        var result = _mtComponentMap.Overload(overload);
        Assert.NotEqual(_mtComponentMap, result);
        Assert.Equal(2, result.Count);
        Assert.Equal(test, result["test"]);
        Assert.Equal(entry, result["entry"]);
    }
}
