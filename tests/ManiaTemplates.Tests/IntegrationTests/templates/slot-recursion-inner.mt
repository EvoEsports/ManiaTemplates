<component>
    <import component="SlotRecursionOuter" as="SlotRecursionOuter"/>

    <template>
        <label text="test" />
        <SlotRecursionOuter>
            <label text="this is root content"/>
            <slot/>
        </SlotRecursionOuter>
        
        <SlotRecursionOuter>
            <SlotRecursionOuter>
                <SlotRecursionOuter>
                    <label text="this is root2 content"/>
                    <slot/>
                </SlotRecursionOuter>
            </SlotRecursionOuter>
        </SlotRecursionOuter>
    </template>
</component>
