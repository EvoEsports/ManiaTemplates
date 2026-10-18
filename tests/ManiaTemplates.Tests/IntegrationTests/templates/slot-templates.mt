<component>
    <import component="SlotsComponent" as="SlotsComponent"/>

    <property type="bool" name="show"/>

    <template>
        <!-- Hidden footer -->
        <SlotsComponent>
            <label text="default"/>
            <template slot="footer" if="show">
                <label text="hidden"/>
            </template>
        </SlotsComponent>
        <!-- Repeated footer -->
        <SlotsComponent>
            <template slot="footer" for="int i = 0; i < 2; i++">
                <label text="footer {{ i }}"/>
            </template>
        </SlotsComponent>
        <!-- Default slot template with condition -->
        <SlotsComponent>
            <template slot="default" if="!show">
                <label text="shown"/>
            </template>
        </SlotsComponent>
    </template>
</component>
