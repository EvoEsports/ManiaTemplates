<component>
    <template>
        <label text="{{ testVariable }}"/>
        <label text="{{ complex.TestString }}"/>
        <label foreach="string test in complex.TestArray" text="{{ test }}"/>
    </template>
</component>