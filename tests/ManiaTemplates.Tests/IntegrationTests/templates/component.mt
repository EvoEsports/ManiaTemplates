<component>
    <import component="Wrapper" as="Wrapper" />
    
    <property type="string" name="text" default="" />
    <property type="double" name="width" default="15" />
    
    <template>
        <Wrapper width="{{ width }}">
            <label size="{{ width }} 3" text="{{ text }}" />
        </Wrapper>
    </template>
</component>