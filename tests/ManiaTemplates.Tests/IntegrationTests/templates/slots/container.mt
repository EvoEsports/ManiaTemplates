<component>
    <property type="double" name="x" default="0.0" />
    <property type="double" name="y" default="0.0" />
    <property type="double" name="width" default="0.0" />
    <property type="double" name="height" default="0.0" />
    <property type="double" name="scale" default="0.0" />
    <property type="bool" name="scrollable" default="false" />
    <property type="bool" name="hidden" default="false" />
    <property type="int" name="zIndex" default="0" />
    <property type="int" name="rotate" default="0" />
    <property type="string" name="className" default="" />

    <template>
        <frame
                id="evosc_container"
                pos="{{ x }} {{ y }}"
                size="{{ width }} {{ height }}"
                scriptevents='{{ scrollable ? "1" : "0" }}'
                z-index="{{ zIndex }}"
                rot="{{ rotate }}"
                scale="{{ scale }}"
                class="{{ className }}"
                hidden='{{ hidden ? "1" : "0" }}'
        >
            <quad size="9999 9999" pos="0 0" if="scrollable" />
            <slot />
        </frame>
    </template>
</component>