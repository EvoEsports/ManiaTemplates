<component>
    <using namespace="System.Collections.Generic"/>

    <import component="TestComponent" as="TestComponent"/>

    <property type="int" name="count"/>
    <property type="List<string>" name="names"/>

    <template>
        <label for="int i = 0; i < count; i++" text="{{ i }} at {{ __index }}"/>
        <!-- Sibling loop with multiple variables -->
        <label for="int i = 10, j = 0; i > j; i -= 4, j++" text="{{ i }}, {{ j }} at {{ __index }}"/>
        <!-- Loop nested in foreach -->
        <frame foreach="string name in names">
            <label for="int i = 0; i < 2; i++" text="{{ name }} {{ i }} at {{ __index }}, {{ __index2 }}"/>
        </frame>
        <!-- Conditions inside the loop are evaluated per iteration -->
        <frame for="int i = 0; i < 3; i++">
            <label if="i % 2 == 0" text="even {{ i }}"/>
        </frame>
        <!-- Loop variables are passed to components and slots -->
        <TestComponent for="int i = 3; i > 1; i--" i="{{ i }}">
            <label text="slot {{ i }} at {{ __index }}"/>
        </TestComponent>
    </template>
</component>
