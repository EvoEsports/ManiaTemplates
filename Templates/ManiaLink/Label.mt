<component>
    <property type="string" name="text" default="NO_TEXT"/>
    <property type="string" name="textfont" default="GameFont"/>
    <property type="double" name="x" default="0.0" />
    <property type="double" name="y" default="0.0" />
    <property type="int" name="z" default="0" />
    <property type="double" name="textsize" default="1.0" />

    <template>
        <label text="{{ text }}" pos="{{ x }} {{ y }}" textsize="{{ textsize }}" textfont="{{ textfont }}" z-index="{{ z }}"/>
    </template>
</component>
