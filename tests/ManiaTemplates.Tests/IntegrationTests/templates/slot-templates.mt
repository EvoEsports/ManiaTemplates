<component>
    <import component="SlotsComponent" as="SlotsComponent"/>

    <property type="bool" name="show"/>

    <template>
        <!-- Hidden footer -->
        <SlotsComponent>
            <label text="default"/>
            <template slot="footer">
                <template if="show">
                    <label text="hidden"/>
                </template>
            </template>
        </SlotsComponent>
        <!-- Repeated footer -->
        <SlotsComponent>
            <template slot="footer">
                <label for="int i = 0; i < 2; i++" text="footer {{ i }}"/>
            </template>
        </SlotsComponent>
        <!-- Default slot template with condition -->
        <SlotsComponent>
            <template slot="default">
                <template if="!show">
                    <label text="shown"/>
                </template>
            </template>
        </SlotsComponent>
    </template>
</component>
