namespace ManiaTemplates.Tests.IntegrationTests;

public class ComplexDataType
{
    public string TestString { get; init; } = "UnitTest";
    
    public string[] TestArray { get; init; } = { "one", "two", "three" };
    
    public IEnumerable<int> TestEnumerable { get; init; } = new List<int> { 3, 6, 9 };
}