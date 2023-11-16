<component>
    <import component="ComponentGlobalVariable" as="ComponentGlobalVariable" />
    
    <template>
        <label text="{{ testVariable }}"/>
        <label text="{{ complex.TestString }}"/>
        <label foreach="string testString in complex.TestArray" text="{{ testString }}"/>
        <label foreach="int testList in complex.TestEnumerable" text="{{ testList }}"/>
        <label foreach="int testListTwo in list" text="{{ testListTwo }}"/>
        <ComponentGlobalVariable />
    </template>
</component>