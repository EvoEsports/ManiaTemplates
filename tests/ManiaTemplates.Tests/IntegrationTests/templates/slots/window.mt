<component>
    <import component="Container" as="Container"/>

    <property type="double" name="width" default="600.0" />
    <property type="double" name="height" default="400.0" />
    <property type="double" name="padding" default="5.0" />

    <template>
        <frame>
            <label text="labelBefore" />

            <Container x="{{ padding }}"
                       y="-{{ padding }}"
                       width="{{ width }}"
                       height="{{ height }}"
            >
                <label text="Next element should be TextInput" />
                <slot />
            </Container>
        </frame>
    </template>
</component>