<component>
    <using namespace="System.Collections.Generic"/>

    <import component="TestComponent" as="TestComponent"/>

    <property type="List<int>" name="numbers"/>
    <property type="List<int>" name="empty"/>

    <template>
        <frame foreach="int n in numbers">
            <label if="n == 1" text="one"/>
            <!-- Comments are allowed within a chain -->
            <label else-if="n == 2" text="two"/>
            <TestComponent else-if="{{ n == 3 }}" i="{{ n }}">
                <label text="three"/>
            </TestComponent>
            <label else="" text="other {{ n }}"/>
        </frame>
        <!-- Loops in branches -->
        <label if="empty.Count == 0" text="no entries"/>
        <frame else="">
            <label foreach="int n in empty" text="entry {{ n }}"/>
        </frame>
        <label if="numbers.Count == 0" text="no entries"/>
        <frame else="">
            <label for="int i = 0; i < 2; i++" text="entry {{ numbers[i] }} at {{ __index }}"/>
        </frame>
        <!-- Chain without matching branch, directly followed by a new chain -->
        <label if="numbers.Count == 0" text="hidden"/>
        <label else-if="numbers.Count > 100" text="hidden"/>
        <label if="numbers.Count > 0" text="new chain"/>
        <label else="" text="hidden"/>
    </template>
</component>
