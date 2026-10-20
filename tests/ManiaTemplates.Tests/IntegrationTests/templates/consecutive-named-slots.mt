<component>
    <import component="SlotsComponent" as="SlotsComponent"/>

    <template>
        <SlotsComponent>
            <label text="default"/>
            <template slot="unknown">
                <label text="dropped"/>
            </template>
            <template slot="footer">
                <label text="footer"/>
            </template>
        </SlotsComponent>
    </template>
</component>
