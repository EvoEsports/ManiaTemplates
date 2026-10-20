<component>
    <using namespace="System.Collections.Generic"/>

    <import component="SlotsComponent" as="SlotsComponent"/>
    <import component="FragmentRoot" as="FragmentRoot"/>

    <property type="List<int>" name="numbers"/>

    <template>
        <template foreach="int n in numbers">
            <label if="n % 2 == 0" text="{{ n }} is even"/>
            <label else="" text="{{ n }} is odd"/>
            <!-- Nested fragment -->
            <template if="n == 2">
                <label text="two at {{ __index }}"/>
            </template>
        </template>
        <template if="numbers.Count == 0">
            <label text="empty"/>
        </template>
        <template else="">
            <label text="not empty"/>
            <label text="{{ numbers.Count }} entries"/>
        </template>
        <template/>
        <!-- Fragments in default slot, next to named slots -->
        <SlotsComponent>
            <template if="numbers.Count > 0">
                <label text="default slot"/>
            </template>
            <template slot="footer">
                <label text="footer"/>
            </template>
            <template slot="unknown">
                <label text="dropped"/>
            </template>
        </SlotsComponent>
        <!-- Fragment as component root does not receive fallthrough attributes -->
        <FragmentRoot data-test="ignored"/>
    </template>
</component>
