<component>
    <property type="double" name="x" default="0.0" />
    <property type="double" name="y" default="0.0" />
    <property type="double" name="w" default="0.0" />
    <property type="double" name="h" default="0.0" />
    <property type="string" name="bg" default="" />

    <template>
        <quad pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}" bgcolor="{{ bg }}" />
    </template>
</component>
