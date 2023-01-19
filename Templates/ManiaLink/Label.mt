<component>
    <property type="string" name="text" default="NO_TEXT"/>
    <property type="double" name="x" default="0.0" />
    <property type="double" name="y" default="0.0" />
    <property type="double" name="size" default="1.0" />

    <template>
        <label text="{{ text }}" pos="{{ x }} {{ y }}" textsize="{{ size }}"/>
    </template>
</component>
