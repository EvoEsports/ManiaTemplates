<component>
    <import component="TestComponent" as="TestComponent" />
    <import component="Wrapper" as="Wrapper" />
    
    <property type="string" name="testVariable" />
    
    <template>
        <TestComponent text="{{ testVariable }}test" />
        <TestComponent text="{{ testVariable }}{{ testVariable }}" />
        <TestComponent text="test" />
        <label text="{{ testVariable }}" />
        <Wrapper width="{{ 123.0 }}">
            <Wrapper width="{{ 77.0 }}">
                <label text="{{ testVariable }}" />
            </Wrapper>
        </Wrapper>
    </template>
</component>