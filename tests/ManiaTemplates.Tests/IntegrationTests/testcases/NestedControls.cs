namespace ManiaTemplates.Tests.IntegrationTests.testcases;

public class NestedControls: ITestData
{
    public string GetFileName()
    {
        return "nested-controls";
    }

    public dynamic GetTestData()
    {
        return new { numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 } };
    }
}
