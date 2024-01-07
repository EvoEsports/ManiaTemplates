<component>
    <import component="SlotRecursionOuter" as="SlotRecursionOuter"/>

    <template>
        <label text="test" />
        <SlotRecursionOuter>
            <label text="this is root content"/>
            <slot/>
        </SlotRecursionOuter>
    </template>
</component>
