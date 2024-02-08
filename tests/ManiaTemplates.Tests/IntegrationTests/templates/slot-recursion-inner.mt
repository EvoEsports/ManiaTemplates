<component>
    <import component="SlotRecursionOuter" as="SlotRecursionOuter"/>
    <import component="SlotRecursionOuterTwo" as="SlotRecursionOuterTwo"/>

    <template>
        <label text="test"/>
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

        <SlotRecursionOuterTwo>
            <SlotRecursionOuter>
                <SlotRecursionOuterTwo>
                    <SlotRecursionOuter>
                        <SlotRecursionOuter>
                            <label text="this is root3 content"/>
                        </SlotRecursionOuter>
                    </SlotRecursionOuter>
                </SlotRecursionOuterTwo>
            </SlotRecursionOuter>
        </SlotRecursionOuterTwo>
    </template>
</component>
