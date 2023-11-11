<component>
    <import component="ComponentGlobalVariable" as="ComponentGlobalVariable" />
    
    <template>
        <label text="{{ testVariable }}"/>
        <label text="{{ complex.TestString }}"/>
        <label foreach="string testString in complex.TestArray" text="{{ testString }}"/>
        <label foreach="int testList in complex.TestEnumerable" text="{{ testList }}"/>
        <ComponentGlobalVariable />
    </template>
</component>