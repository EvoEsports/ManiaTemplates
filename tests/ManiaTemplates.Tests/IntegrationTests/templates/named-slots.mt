<component>
    <import component="SlotsComponent" as="SlotsComponent"/>

    <property type="string" name="testVariable"/>

    <template>
        <!-- Section 1 -->
        <SlotsComponent>
            <label text="test"/>

            <template slot="footer">
                <label text="{{ testVariable }}"/>
            </template>
        </SlotsComponent>
        
        <!-- Section 2 -->
        <SlotsComponent>
            <SlotsComponent>
                <label text="test2"/>

                <template slot="footer">
                    <label text="{{ testVariable }}_footer2"/>
                </template>
            </SlotsComponent>

            <template slot="footer">
                <label text="test3"/>
                <SlotsComponent>
                    <label text="test4"/>
                </SlotsComponent>
            </template>
        </SlotsComponent>
    </template>
</component>
