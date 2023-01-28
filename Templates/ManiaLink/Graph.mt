<component>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w" default="0.0"/>
    <property type="double" name="h" default="0.0"/>
    <property type="string" name="halign" default="left"/>
    <property type="string" name="valign" default="center"/>
    <property type="int" name="z" default="0"/>
    <property type="int" name="events" default="0"/>

    <template>
        <graph pos="{{ x }} {{ y }}"
               size="{{ w }} {{ h }}"
               halign="{{ halign }}"
               valign="{{ valign }}"
               z-index="{{ z }}"
               ScriptEvents="{{ events }}"
        >
            <slot/>
        </graph>
    </template>
</component>
