<component>
    <import component="SlotRecursionOuterTwo" as="SlotRecursionOuterTwo"/>

    <template>
        <SlotRecursionOuterTwo>
            <label text="this is parent content"/>
            <slot/>
        </SlotRecursionOuterTwo>
    </template>
</component>
