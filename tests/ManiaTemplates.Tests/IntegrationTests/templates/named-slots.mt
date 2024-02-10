<component>
    <import component="SlotsComponent" as="SlotsComponent"/>

    <property type="string" name="testVariable"/>
    <property type="string" name="test2Variable" default="abcd"/>
    <property type="string" name="test3Variable" default="qwer"/>
    <property type="string" name="test4Variable" default="asdf"/>

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
                <label text="{{ test2Variable }}"/>

                <template slot="footer">
                    <label text="{{ test3Variable }}_{{ testVariable }}_footer2"/>
                </template>
            </SlotsComponent>

            <template slot="footer">
                <label text="{{ test2Variable }}_{{ test3Variable }}"/>
                <SlotsComponent>
                    <label text="{{ test4Variable }}"/>
                </SlotsComponent>
            </template>
        </SlotsComponent>
    </template>
</component>
