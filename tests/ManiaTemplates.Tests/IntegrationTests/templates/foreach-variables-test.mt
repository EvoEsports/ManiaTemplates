<component>
    <using namespace="System.Collections.Generic"/>

    <import component="TestComponent" as="TestComponent"/>

    <property type="Dictionary<string, int>" name="scores"/>
    <property type="List<(string, (int, bool))>" name="entries"/>

    <template>
        <TestComponent foreach="KeyValuePair<string, int> score in scores" i="{{ score.Value }}">
            <label text="{{ score.Key }} at {{ __index }}"/>
        </TestComponent>
        <TestComponent foreach="(string name, (int value, bool _)) in entries" i="{{ value }}">
            <label text="{{ name }} at {{ __index }}"/>
        </TestComponent>
    </template>
</component>
