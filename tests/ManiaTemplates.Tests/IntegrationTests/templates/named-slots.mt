<component>
    <import component="SlotsComponent" as="SlotsComponent" />
    
    <property type="string" name="testVariable" />
    
    <template>
        <SlotsComponent>
            <label text="test" />
            
            <template slot="footer">
                <label text="{{ testVariable }}" />
            </template>
        </SlotsComponent>
    </template>
</component>
