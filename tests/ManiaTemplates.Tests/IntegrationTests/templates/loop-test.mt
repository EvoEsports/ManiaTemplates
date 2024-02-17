<component>
    <using namespace="System.Linq"/>

    <import component="TestComponent" as="TestComponent"/>
    <import component="TestComponentWithLoop" as="TestComponentWithLoop"/>

    <template>
        <TestComponent foreach="int i in Enumerable.Range(1, 3)">
            <TestComponent i="{{ i * 10 }}">
                <label text="outer_{{ i }}"/>
            </TestComponent>
        </TestComponent>
        <!-- End Loop 1 -->
        <TestComponent foreach="int i in Enumerable.Range(1, 3)">
            <TestComponent i="{{ i }}">
                <TestComponent>
                    <label text="outer" />
                </TestComponent>
            </TestComponent>
        </TestComponent>
        <!-- End Loop 2 -->
        <TestComponent foreach="int i in Enumerable.Range(1, 2)" i="{{ __index * 1000 }}">
            <TestComponent foreach="int j in Enumerable.Range(1, 2)" i="{{ __index2 * 100 }}">
                <label text="outer_{{ __index }}_{{ __index2 }}_{{ i }}_{{ j }}"/>
            </TestComponent>
        </TestComponent>
        <!-- End Loop 3 -->
        <TestComponentWithLoop foreach="int i in Enumerable.Range(1, 2)">
            <label text="outer_{{ __index }}_{{ i }}"/>
        </TestComponentWithLoop>
    </template>
</component>