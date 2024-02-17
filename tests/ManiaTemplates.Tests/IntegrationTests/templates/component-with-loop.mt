<component>
    <using namespace="System.Linq"/>

    <template>
        <frame foreach="int i in Enumerable.Range(1, 3)">
            <label text="inner_{{ __index }}_{{ i }}"/>
            <slot/>
        </frame>
    </template>
</component>