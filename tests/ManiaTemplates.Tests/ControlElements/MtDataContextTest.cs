using ManiaTemplates.ControlElements;

namespace ManiaTemplates.Tests.ControlElements;

public class MtDataContextTest
{
    [Fact]
    public void Should_Create_New_Context_With_Previous_Variables()
    {
        var oldContext = new MtDataContext
        {
            { "__index", "int" },
            { "i", "int" },
        };

        var newContext = new MtDataContext
        {
            { "__index2", "int" },
            { "j", "int" },
            { "i", "int" },
        };

        var merged = oldContext.NewContext(newContext);
        Assert.Equal(4, merged.Count);
        Assert.Equal("int", merged["__index2"]);
    }
}