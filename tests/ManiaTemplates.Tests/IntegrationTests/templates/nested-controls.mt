<!-- should not break parsing -->
<component>
    <property type="List<int>" name="numbers"/>
    <property type="bool" name="enabled" default="true"/>
    
    <template>
        <Frame if="enabled" foreach="int i in numbers" x="{{ 10 * __index }}">
            <Label if="i &lt; numbers.Count" foreach="int j in numbers.GetRange(0, i)" text="{{ i }}, {{ j }} at index {{ __index }}, {{ __index2 }}"/>
        </Frame>
        <Frame>
            <Frame>
                <test>
                    <Label text="Nested controls"/>
                </test>
            </Frame>
        </Frame>
    </template>
</component>
