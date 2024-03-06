<component>
    <using namespace="System.Linq" />
    <import component="Wrapper" as="Wrapper" />
    <import component="MultiChild" as="MultiChild" />
    
    <template>
        <Wrapper foreach="int i in Enumerable.Range(1, 3)" 
                 data-test="unit{{ __index }}" 
                 datatest="{{ i }}" />
        <MultiChild data-test="nope" />
    </template>
</component>
