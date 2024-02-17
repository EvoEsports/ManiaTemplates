<component>
    <using namespace="System.Linq"/>
    
    <import component="LoopComponent" as="LoopComponent" />
    
    <template>
        <LoopComponent foreach="int i in Enumerable.Range(1, 3)">
            <LoopComponent i="{{ i * 10 }}">
                <label text="outer_{{ i }}" />
            </LoopComponent>
        </LoopComponent>
    </template>
</component>