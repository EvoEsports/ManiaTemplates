<component>
    <using namespace="System.Linq" />
    
    <import component="Wrapper" as="Wrapper" />
    
    <template>
        <Wrapper foreach="int i in Enumerable.Range(1, 3)" data-test="unit{{ __index }}" datatest="{{ i }}" />
    </template>
</component>