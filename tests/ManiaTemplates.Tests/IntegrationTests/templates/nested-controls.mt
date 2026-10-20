<!-- should not break parsing -->
<component>
    <property type="List<int>" name="numbers"/>
    <property type="bool" name="enabled" default="true"/>
    
    <template>
        <frame if="enabled">
            <Frame foreach="int i in numbers" x="{{ 10 * __index }}">
                <frame if="i < numbers.Count">
                    <Label foreach="int j in numbers.GetRange(0, i)" text="{{ i }}, {{ j }} at index {{ __index }}, {{ __index2 }}"/>
                </frame>
            </Frame>
        </frame>
        <Frame>
            <Frame>
                <test>
                    <Label text="Nested controls"/>
                </test>
            </Frame>
        </Frame>
    </template>
</component>
