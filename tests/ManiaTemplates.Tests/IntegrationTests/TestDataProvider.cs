using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace ManiaTemplates.Tests.IntegrationTests;

public class TestDataProvider: IEnumerable<object[]>
{
    private static IEnumerable<ITestData> GetFiles()
    {
        var type = typeof(ITestData);
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => type.IsAssignableFrom(p) && p != type).Select(t => (ITestData) Activator.CreateInstance(t)!);
    }
    public IEnumerator<object[]> GetEnumerator()
    {
        return GetFiles().Select(s =>
        {
            return new[]
            {
                File.ReadAllText($"IntegrationTests/templates/{s.GetFileName()}.mt"), 
                s.GetTestData(),
                File.ReadAllText($"IntegrationTests/expected/{s.GetFileName()}.xml")
            };
        }).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
