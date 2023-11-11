<component>
    <import component="ComponentGlobalVariable" as="ComponentGlobalVariable" />
    
    <template>
        <label text="{{ testVariable }}"/>
        <label text="{{ complex.TestString }}"/>
        <label foreach="string test in complex.TestArray" text="{{ test }}"/>
        <ComponentGlobalVariable />
    </template>
</component>