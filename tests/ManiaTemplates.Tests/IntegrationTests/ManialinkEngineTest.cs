using ManiaTemplates.Exceptions;

namespace ManiaTemplates.Tests.IntegrationTests;

public class ManialinkEngineTest
{
    private readonly ManiaTemplateEngine _maniaTemplateEngine = new();


    [Theory]
    [ClassData(typeof(TestDataProvider))]
    public void Should_Convert_Templates_To_Result(string template, dynamic data, string expected)
    {
        _maniaTemplateEngine.AddTemplateFromString("test", template);
        _maniaTemplateEngine.PreProcess("test", new[] { typeof(ManiaTemplateEngine).Assembly });

        var pendingResult =
            _maniaTemplateEngine.RenderAsync("test", data, new[] { typeof(ManiaTemplateEngine).Assembly });
        var result = pendingResult.Result;

        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Pass_Global_Variables()
    {
        var componentTemplate = File.ReadAllText("IntegrationTests/templates/global-variables.mt");
        var componentWithGlobalVariable = File.ReadAllText("IntegrationTests/templates/component-using-gvar.mt");
        var expectedOutput = File.ReadAllText("IntegrationTests/expected/global-variables.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("ComponentGlobalVariable", componentWithGlobalVariable);
        _maniaTemplateEngine.AddTemplateFromString("GlobalVariables", componentTemplate);

        dynamic dynamicObject = new DynamicDictionary();
        dynamicObject.DynamicProperty = "UnitTest";
        AddGlobalVariable("dynamicObject", dynamicObject);


        _maniaTemplateEngine.GlobalVariables["testVariable"] = "unittest";
        _maniaTemplateEngine.GlobalVariables["complex"] = new ComplexDataType();
        _maniaTemplateEngine.GlobalVariables["list"] = new List<int> { 3, 6, 9 };

        var pendingResult = _maniaTemplateEngine.RenderAsync("GlobalVariables", new { }, assemblies);
        var result = pendingResult.Result;

        Assert.Equal(expectedOutput, result, ignoreLineEndingDifferences: true);
    }

    private void AddGlobalVariable(string name, object value)
    {
        _maniaTemplateEngine.GlobalVariables.AddOrUpdate(name, value, (s, o) => value);
    }

    [Fact]
    public void Should_Fill_Named_Slots()
    {
        var namedSlotsTemplate = File.ReadAllText("IntegrationTests/templates/named-slots.mt");
        var componentTemplate = File.ReadAllText("IntegrationTests/templates/component-multi-slot.mt");
        var expected = File.ReadAllText($"IntegrationTests/expected/named-slots.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("NamedSlots", namedSlotsTemplate);
        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", componentTemplate);

        var result = _maniaTemplateEngine.RenderAsync("NamedSlots", new
        {
            testVariable = "UnitTest"
        }, assemblies).Result;

        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Throw_Exception_For_Duplicate_Slot_In_Source_Template()
    {
        var namedSlotsTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/named-slots.mt");
        var componentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/component-duplicate-slot.mt");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("NamedSlots", namedSlotsTemplate);
        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", componentTemplate);

        await Assert.ThrowsAsync<DuplicateSlotException>(() =>
            _maniaTemplateEngine.RenderAsync("NamedSlots", new { testVariable = "UnitTest" }, assemblies));
    }

    [Fact]
    public async void Should_Render_Component_Without_Content_For_Slot()
    {
        var slotRecursionOuterTwoTemplate =
            await File.ReadAllTextAsync("IntegrationTests/templates/slot-recursion-outer-two.mt");
        var slotRecursionOuterTemplate =
            await File.ReadAllTextAsync("IntegrationTests/templates/slot-recursion-outer.mt");
        var slotRecursionInnerTemplate =
            await File.ReadAllTextAsync("IntegrationTests/templates/slot-recursion-inner.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/single-slot-unfilled.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("SlotRecursionOuterTwo", slotRecursionOuterTwoTemplate);
        _maniaTemplateEngine.AddTemplateFromString("SlotRecursionOuter", slotRecursionOuterTemplate);
        _maniaTemplateEngine.AddTemplateFromString("SlotRecursionInner", slotRecursionInnerTemplate);

        var template = _maniaTemplateEngine.RenderAsync("SlotRecursionInner", new { }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Pass_Properties_To_Components_And_Slots()
    {
        var propertyTestTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/property-test.mt");
        var testWrapperTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/wrapper.mt");
        var testComponentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/component.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/property-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("PropertyTest", propertyTestTemplate);
        _maniaTemplateEngine.AddTemplateFromString("Wrapper", testWrapperTemplate);
        _maniaTemplateEngine.AddTemplateFromString("TestComponent", testComponentTemplate);

        var template = _maniaTemplateEngine.RenderAsync("PropertyTest", new
        {
            testVariable = "integration"
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Append_Parent_Attributes_To_Single_Component_Child()
    {
        var fallthroughComponent = await File.ReadAllTextAsync("IntegrationTests/templates/fallthrough-component.mt");
        var fallthroughWrapper = await File.ReadAllTextAsync("IntegrationTests/templates/fallthrough-wrapper.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/fallthrough.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("FallthroughComponent", fallthroughComponent);
        _maniaTemplateEngine.AddTemplateFromString("FallthroughWrapper", fallthroughWrapper);

        var template = _maniaTemplateEngine.RenderAsync("FallthroughWrapper", new
        {
            testString = "unit",
            index = -1
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Not_Append_Parent_Attributes_To_Multiple_Component_Children()
    {
        var fallthroughComponent = await File.ReadAllTextAsync("IntegrationTests/templates/fallthrough-component-multi-child.mt");
        var fallthroughWrapper = await File.ReadAllTextAsync("IntegrationTests/templates/fallthrough-wrapper.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/fallthrough-multi-child.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("FallthroughComponent", fallthroughComponent);
        _maniaTemplateEngine.AddTemplateFromString("FallthroughWrapper", fallthroughWrapper);

        var template = _maniaTemplateEngine.RenderAsync("FallthroughWrapper", new { }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }
}