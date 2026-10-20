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
    public async void Should_Fill_Consecutive_Named_Slots()
    {
        var componentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/component-multi-slot.mt");
        var namedSlotsTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/consecutive-named-slots.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/consecutive-named-slots.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", componentTemplate);
        _maniaTemplateEngine.AddTemplateFromString("ConsecutiveNamedSlots", namedSlotsTemplate);

        var result = _maniaTemplateEngine.RenderAsync("ConsecutiveNamedSlots", new { }, assemblies).Result;
        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Apply_Conditions_And_Loops_To_Slot_Templates()
    {
        var componentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/component-multi-slot.mt");
        var slotTemplates = await File.ReadAllTextAsync("IntegrationTests/templates/slot-templates.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/slot-templates.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", componentTemplate);
        _maniaTemplateEngine.AddTemplateFromString("SlotTemplates", slotTemplates);

        var result = _maniaTemplateEngine.RenderAsync("SlotTemplates", new { show = false }, assemblies).Result;
        Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("<template slot=\"footer\" if=\"true\"><label/></template>", null)]
    [InlineData("<template slot=\"footer\" foreach=\"int i in list\"><label/></template>", null)]
    [InlineData("<label/><template slot=\"footer\" text=\"ignored\"><label/></template>", null)]
    [InlineData("<template slot=\"default\" if=\"true\"><label/></template>", null)]
    [InlineData("<label/>", "<frame><slot if=\"true\"/><slot name=\"footer\"/></frame>")]
    [InlineData("<label/>", "<frame><label if=\"true\"/><slot else=\"\"/><slot name=\"footer\"/></frame>")]
    [InlineData("<label/>", "<frame><slot/><slot name=\"footer\" for=\"int i = 0; i < 2; i++\"/></frame>")]
    public async void Should_Throw_Exception_For_Control_Flow_On_Slots(string slotContent, string? componentTemplate)
    {
        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent",
            $"<component><template>{componentTemplate ?? "<frame><slot/><slot name=\"footer\"/></frame>"}</template></component>");
        _maniaTemplateEngine.AddTemplateFromString("ControlFlowOnSlot", $"""
            <component>
                <import component="SlotsComponent" as="SlotsComponent"/>
                <property type="List<int>" name="list"/>
                <template>
                    <SlotsComponent>{slotContent}</SlotsComponent>
                </template>
            </component>
            """);

        await Assert.ThrowsAsync<UnsupportedAttributeException>(() =>
            _maniaTemplateEngine.RenderAsync("ControlFlowOnSlot", new { list = new List<int>() },
                new[] { typeof(ManiaTemplateEngine).Assembly }));
    }

    [Fact]
    public async void Should_Throw_Exception_For_Multiple_Templates_Of_Same_Slot()
    {
        var componentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/component-multi-slot.mt");
        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", componentTemplate);
        _maniaTemplateEngine.AddTemplateFromString("MultipleSlotTemplates", """
            <component>
                <import component="SlotsComponent" as="SlotsComponent"/>
                <template>
                    <SlotsComponent>
                        <template slot="footer"><label text="first"/></template>
                        <template slot="footer"><label text="second"/></template>
                    </SlotsComponent>
                </template>
            </component>
            """);

        await Assert.ThrowsAsync<DuplicateSlotException>(() =>
            _maniaTemplateEngine.RenderAsync("MultipleSlotTemplates", new { },
                new[] { typeof(ManiaTemplateEngine).Assembly }));
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
    public async void Should_Fill_All_Slots()
    {
        var baseTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/slots/base.mt");
        var containerTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/slots/container.mt");
        var windowTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/slots/window.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/slots/manialink.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("Base", baseTemplate);
        _maniaTemplateEngine.AddTemplateFromString("Container", containerTemplate);
        _maniaTemplateEngine.AddTemplateFromString("Window", windowTemplate);

        var template = _maniaTemplateEngine.RenderAsync("Base", new { }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Component_Properties_Should_Not_Collide_With_Loop_Variables()
    {
        var testComponent = await File.ReadAllTextAsync("IntegrationTests/templates/loop-test-component.mt");
        var testComponentWithLoop = await File.ReadAllTextAsync("IntegrationTests/templates/component-with-loop.mt");
        var containerTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/loop-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/loop-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("TestComponent", testComponent);
        _maniaTemplateEngine.AddTemplateFromString("TestComponentWithLoop", testComponentWithLoop);
        _maniaTemplateEngine.AddTemplateFromString("LoopTest", containerTemplate);

        var template = _maniaTemplateEngine.RenderAsync("LoopTest", new { }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Render_For_Loops()
    {
        var testComponent = await File.ReadAllTextAsync("IntegrationTests/templates/loop-test-component.mt");
        var forLoopTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/for-loop-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/for-loop-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("TestComponent", testComponent);
        _maniaTemplateEngine.AddTemplateFromString("ForLoopTest", forLoopTemplate);

        var template = _maniaTemplateEngine.RenderAsync("ForLoopTest", new
        {
            count = 3,
            names = new List<string> { "a", "b" }
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Pass_Foreach_Variables_To_Slots()
    {
        var testComponent = await File.ReadAllTextAsync("IntegrationTests/templates/loop-test-component.mt");
        var foreachTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/foreach-variables-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/foreach-variables-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("TestComponent", testComponent);
        _maniaTemplateEngine.AddTemplateFromString("ForeachVariablesTest", foreachTemplate);

        var template = _maniaTemplateEngine.RenderAsync("ForeachVariablesTest", new
        {
            scores = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
            entries = new List<(string, (int, bool))> { ("x", (5, true)), ("y", (7, false)) }
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async void Should_Render_Condition_Chains()
    {
        var testComponent = await File.ReadAllTextAsync("IntegrationTests/templates/loop-test-component.mt");
        var conditionsTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/conditions-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/conditions-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("TestComponent", testComponent);
        _maniaTemplateEngine.AddTemplateFromString("ConditionsTest", conditionsTemplate);

        var template = _maniaTemplateEngine.RenderAsync("ConditionsTest", new
        {
            numbers = new List<int> { 1, 2, 3, 4 },
            empty = new List<int>()
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("<label else=\"\"/>")]
    [InlineData("<label if=\"true\"/>text<label else=\"\"/>")]
    [InlineData("<label if=\"true\"/><label else=\"\"/><label else=\"\"/>")]
    [InlineData("<label if=\"true\"/><label/><label else-if=\"true\"/>")]
    [InlineData("<label foreach=\"int i in list\" if=\"i > 0\"/>")]
    [InlineData("<label if=\"true\"/><label else=\"\" foreach=\"int i in list\"/>")]
    [InlineData("<label if=\"true\"/><label else-if=\"true\" for=\"int i = 0; i < 1; i++\"/>")]
    [InlineData("<label if=\"true\" else=\"\"/>")]
    [InlineData("<label if=\"true\"/><label else=\"false\"/>")]
    public async void Should_Throw_Exception_For_Invalid_Condition_Chain(string templateContent)
    {
        _maniaTemplateEngine.AddTemplateFromString("InvalidCondition",
            $"<component><property type=\"List<int>\" name=\"list\"/><template>{templateContent}</template></component>");

        await Assert.ThrowsAsync<InvalidConditionException>(() =>
            _maniaTemplateEngine.RenderAsync("InvalidCondition", new { list = new List<int>() },
                new[] { typeof(ManiaTemplateEngine).Assembly }));
    }

    [Fact]
    public async void Should_Render_Fragment_Children_Only()
    {
        var slotsComponent = await File.ReadAllTextAsync("IntegrationTests/templates/component-multi-slot.mt");
        var fragmentRootComponent = await File.ReadAllTextAsync("IntegrationTests/templates/component-fragment-root.mt");
        var fragmentTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/fragment-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/fragment-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("SlotsComponent", slotsComponent);
        _maniaTemplateEngine.AddTemplateFromString("FragmentRoot", fragmentRootComponent);
        _maniaTemplateEngine.AddTemplateFromString("FragmentTest", fragmentTemplate);

        var template = _maniaTemplateEngine.RenderAsync("FragmentTest", new
        {
            numbers = new List<int> { 1, 2 }
        }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("<template pos=\"1 2\"><label/></template>")]
    [InlineData("<frame><template slot=\"footer\"><label/></template></frame>")]
    public async void Should_Throw_Exception_For_Fragment_Attributes(string templateContent)
    {
        _maniaTemplateEngine.AddTemplateFromString("InvalidFragment",
            $"<component><template>{templateContent}</template></component>");

        await Assert.ThrowsAsync<UnsupportedAttributeException>(() =>
            _maniaTemplateEngine.RenderAsync("InvalidFragment", new { },
                new[] { typeof(ManiaTemplateEngine).Assembly }));
    }

    [Fact]
    public void Should_Not_Pass_Loop_Variables_Into_Component_Templates()
    {
        //The component uses a component with a slot and declares a loop variable with the same name as the outer loop
        _maniaTemplateEngine.AddTemplateFromString("LoopComponent", """
            <component>
                <template>
                    <Frame>
                        <label foreach="int i in new[] { 7 }" text="inner {{ i }}"/>
                    </Frame>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("ComponentInLoop", """
            <component>
                <import component="LoopComponent" as="LoopComponent"/>
                <template>
                    <LoopComponent foreach="int i in new[] { 1, 2 }"/>
                    <frame foreach="int i in new[] { 1, 2 }">
                        <LoopComponent/>
                    </frame>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("ComponentInLoop", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Equal(4, template.Split("<label text=\"inner 7\" />").Length - 1);
    }

    [Fact]
    public async void Should_Throw_Exception_For_Variable_Reused_In_Nested_Loop()
    {
        _maniaTemplateEngine.AddTemplateFromString("ReusedVariable", """
            <component>
                <template>
                    <frame foreach="int i in new[] { 1, 2 }">
                        <label foreach="int i in new[] { 3, 4 }" text="{{ i }}"/>
                    </frame>
                </template>
            </component>
            """);

        var exception = await Assert.ThrowsAsync<ParsingForeachLoopFailedException>(() =>
            _maniaTemplateEngine.RenderAsync("ReusedVariable", new { }, new[] { typeof(ManiaTemplateEngine).Assembly }));
        Assert.Contains("The variable i is already declared", exception.Message);
    }

    [Fact]
    public void Should_Pass_Types_Containing_String_To_Components()
    {
        _maniaTemplateEngine.AddTemplateFromString("StringTypes", """
            <component>
                <property type="List<string>" name="names"/>
                <property type="string" name="separator" default=", "/>
                <template>
                    <label text="{{ string.Join(separator, names) }}"/>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("StringTypesUsage", """
            <component>
                <import component="StringTypes" as="StringTypes"/>
                <template>
                    <StringTypes names="{{ new List<string> { &quot;a&quot;, &quot;b&quot; } }}"/>
                    <StringTypes names="{{ new List<string> { &quot;c&quot;, &quot;d&quot; } }}" separator=" and "/>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("StringTypesUsage", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Contains("<label text=\"a, b\" />", template);
        Assert.Contains("<label text=\"c and d\" />", template);
    }

    [Fact]
    public async void Should_Pass_Slot_Arguments_To_Slot_Contents()
    {
        var itemListComponent = await File.ReadAllTextAsync("IntegrationTests/templates/item-list.mt");
        var slotArgumentsTemplate = await File.ReadAllTextAsync("IntegrationTests/templates/slot-arguments-test.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/slot-arguments-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly };

        _maniaTemplateEngine.AddTemplateFromString("ItemList", itemListComponent);
        _maniaTemplateEngine.AddTemplateFromString("SlotArgumentsTest", slotArgumentsTemplate);

        var template = _maniaTemplateEngine.RenderAsync("SlotArgumentsTest", new { prefix = "P" }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Should_Pass_Slot_Arguments_At_Every_Position_Of_Slot()
    {
        _maniaTemplateEngine.AddTemplateFromString("RepeatedSlot", """
            <component>
                <property type="string" name="title"/>
                <template>
                    <template foreach="int i in new[] { 1, 2 }">
                        <slot value="{{ title }}"/>
                    </template>
                    <slot value="{{ title }}"/>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("RepeatedSlotUsage", """
            <component>
                <import component="RepeatedSlot" as="RepeatedSlot"/>
                <template>
                    <RepeatedSlot title="T">
                        <label text="{{ value }}"/>
                    </RepeatedSlot>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("RepeatedSlotUsage", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Equal(3, template.Split("<label text=\"T\" />").Length - 1);
    }

    [Fact]
    public void Should_Pass_Slot_Arguments_When_Component_Is_Used_Twice()
    {
        _maniaTemplateEngine.AddTemplateFromString("TwiceUsed", """
            <component>
                <property type="string" name="title"/>
                <template>
                    <slot value="{{ title }}"/>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("TwiceUsedUsage", """
            <component>
                <import component="TwiceUsed" as="TwiceUsed"/>
                <template>
                    <TwiceUsed title="A"><label text="first {{ value }}"/></TwiceUsed>
                    <TwiceUsed title="B"><label text="second {{ value }}"/></TwiceUsed>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("TwiceUsedUsage", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Contains("<label text=\"first A\" />", template);
        Assert.Contains("<label text=\"second B\" />", template);
    }

    [Fact]
    public void Should_Pass_Slot_Arguments_Through_Forwarded_Slot()
    {
        _maniaTemplateEngine.AddTemplateFromString("ForwardInner", """
            <component>
                <property type="string" name="label" default="I"/>
                <template>
                    <frame><slot inner="{{ label }}"/></frame>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("ForwardOuter", """
            <component>
                <import component="ForwardInner" as="ForwardInner"/>
                <property type="string" name="y"/>
                <template>
                    <ForwardInner><slot x="{{ y }}"/></ForwardInner>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("ForwardUsage", """
            <component>
                <import component="ForwardOuter" as="ForwardOuter"/>
                <template>
                    <ForwardOuter y="Y"><label text="{{ x }}"/></ForwardOuter>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("ForwardUsage", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Contains("<label text=\"Y\" />", template);
    }

    [Fact]
    public void Should_Pass_Arguments_To_Named_Slots()
    {
        _maniaTemplateEngine.AddTemplateFromString("NamedArgs", """
            <component>
                <property type="int" name="count"/>
                <template>
                    <slot name="footer" total="{{ count }}"/>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("NamedArgsUsage", """
            <component>
                <import component="NamedArgs" as="NamedArgs"/>
                <template>
                    <NamedArgs count="5">
                        <template slot="footer"><label text="total {{ total }}"/></template>
                    </NamedArgs>
                </template>
            </component>
            """);

        var template = _maniaTemplateEngine.RenderAsync("NamedArgsUsage", new { },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Contains("<label text=\"total 5\" />", template);
    }

    [Fact]
    public void Should_Pass_Slot_Arguments_Of_Generic_Types()
    {
        _maniaTemplateEngine.AddTemplateFromString("GenericArgs", """
            <component>
                <property type="Dictionary<string, List<int>>" name="data"/>
                <template>
                    <slot map="{{ data }}"/>
                </template>
            </component>
            """);
        _maniaTemplateEngine.AddTemplateFromString("GenericArgsUsage", """
            <component>
                <import component="GenericArgs" as="GenericArgs"/>
                <property type="Dictionary<string, List<int>>" name="source"/>
                <template>
                    <GenericArgs data="{{ source }}"><label text="{{ map.Count }}"/></GenericArgs>
                </template>
            </component>
            """);

        var source = new Dictionary<string, List<int>> { ["a"] = new() { 1, 2 }, ["b"] = new() { 3 } };
        var template = _maniaTemplateEngine.RenderAsync("GenericArgsUsage", new { source },
            new[] { typeof(ManiaTemplateEngine).Assembly }).Result;

        Assert.Contains("<label text=\"2\" />", template);
    }

    [Theory]
    [InlineData("<slot item=\"{{ title + 1 }}\"/>", "<label/>")]
    [InlineData("<slot item=\"{{ unknown }}\"/>", "<label/>")]
    [InlineData("<slot item=\"title\"/>", "<label/>")]
    [InlineData("<slot __item=\"{{ title }}\"/>", "<label/>")]
    [InlineData("<slot item=\"{{ title }}\"/><slot/>", "<label/>")]
    public async void Should_Throw_Exception_For_Invalid_Slot_Arguments(string componentTemplate, string usage)
    {
        _maniaTemplateEngine.AddTemplateFromString("SlotComponent",
            $"<component><property type=\"string\" name=\"title\"/><template>{componentTemplate}</template></component>");
        _maniaTemplateEngine.AddTemplateFromString("InvalidSlotArguments", $"""
            <component>
                <import component="SlotComponent" as="SlotComponent"/>
                <property type="List<string>" name="names"/>
                <template>
                    <SlotComponent title="T">{usage}</SlotComponent>
                </template>
            </component>
            """);

        await Assert.ThrowsAsync<InvalidSlotArgumentException>(() =>
            _maniaTemplateEngine.RenderAsync("InvalidSlotArguments", new { names = new List<string>() },
                new[] { typeof(ManiaTemplateEngine).Assembly }));
    }

    [Fact]
    public async void Should_Throw_Exception_For_Slot_Argument_Named_Like_Variable()
    {
        var itemListComponent = await File.ReadAllTextAsync("IntegrationTests/templates/item-list.mt");
        _maniaTemplateEngine.AddTemplateFromString("ItemList", itemListComponent);
        _maniaTemplateEngine.AddTemplateFromString("SlotArgumentConflict", """
            <component>
                <import component="ItemList" as="ItemList"/>
                <template>
                    <ItemList foreach="string item in new List<string>()" items="{{ new List<string>() }}">
                        <label text="{{ item }}"/>
                    </ItemList>
                </template>
            </component>
            """);

        var exception = await Assert.ThrowsAsync<InvalidSlotArgumentException>(() =>
            _maniaTemplateEngine.RenderAsync("SlotArgumentConflict", new { }, new[] { typeof(ManiaTemplateEngine).Assembly }));
        Assert.Contains("The argument item passed to the slot default of <ItemList> conflicts", exception.Message);
    }

    [Fact]
    public async void Should_Throw_Exception_For_Loop_Variable_Named_Like_Slot_Argument()
    {
        var itemListComponent = await File.ReadAllTextAsync("IntegrationTests/templates/item-list.mt");
        _maniaTemplateEngine.AddTemplateFromString("ItemList", itemListComponent);
        _maniaTemplateEngine.AddTemplateFromString("SlotLoopVariable", """
            <component>
                <import component="ItemList" as="ItemList"/>
                <template>
                    <ItemList items="{{ new List<string>() }}">
                        <label foreach="string item in new List<string>()" text="{{ item }}"/>
                    </ItemList>
                </template>
            </component>
            """);

        await Assert.ThrowsAsync<ParsingForeachLoopFailedException>(() =>
            _maniaTemplateEngine.RenderAsync("SlotLoopVariable", new { }, new[] { typeof(ManiaTemplateEngine).Assembly }));
    }

    [Fact]
    public async void Should_Pass_Unmapped_Node_Attributes_To_Component_Root_Element()
    {
        var wrapperComponent = await File.ReadAllTextAsync("IntegrationTests/templates/wrapper.mt");
        var fallthroughAttributesTestComponent = await File.ReadAllTextAsync("IntegrationTests/templates/fallthrough-attributes.mt");
        var multiChildComponent = await File.ReadAllTextAsync("IntegrationTests/templates/component-multiple-elements.mt");
        var expected = await File.ReadAllTextAsync("IntegrationTests/expected/fallthrough-test.xml");
        var assemblies = new[] { typeof(ManiaTemplateEngine).Assembly, typeof(ComplexDataType).Assembly };
        
        _maniaTemplateEngine.AddTemplateFromString("Wrapper", wrapperComponent);
        _maniaTemplateEngine.AddTemplateFromString("MultiChild", multiChildComponent);
        _maniaTemplateEngine.AddTemplateFromString("FallthroughTest", fallthroughAttributesTestComponent);
        
        var template = _maniaTemplateEngine.RenderAsync("FallthroughTest", new { }, assemblies).Result;
        Assert.Equal(expected, template, ignoreLineEndingDifferences: true);
    }
}