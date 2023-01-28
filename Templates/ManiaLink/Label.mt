<component>
    <property type="string" name="halign" default="left"/>
    <property type="string" name="valign" default="center"/>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w" default="0.0"/>
    <property type="double" name="h" default="0.0"/>
    <property type="double" name="opacity" default="1.0"/>
    <property type="int" name="z" default="0"/>
    <property type="int" name="events" default="0"/>
    <property type="string" name="action"/>
    <property type="string" name="url"/>
    <property type="string" name="manialink"/>

    <property type="string" name="style"/>
    <property type="string" name="textfont" default="GameFont"/>
    <property type="double" name="textsize" default="1.0"/>
    <property type="string" name="textcolor"/>
    <property type="string" name="focusareacolor1"/>
    <property type="string" name="focusareacolor2"/>

    <property type="string" name="text"/>
    <property type="string" name="textprefix"/>
    <property type="int" name="bold" default="0"/>
    <property type="int" name="autonewline" default="0"/>
    <property type="int" name="maxline" default="0"/>
    <property type="int" name="translate"/>
    <property type="string" name="textid"/>

    <template>
        <label text="{{ text }}"
               textprefix="{{ textprefix }}"
               style="{{ style }}"
               pos="{{ x }} {{ y }}"
               size="{{ w }} {{ h }}"
               textsize="{{ textsize }}"
               textfont="{{ textfont }}"
               textcolor="{{ textcolor }}"
               bold="{{ bold }}"
               autonewline="{{ autonewline }}"
               maxline="{{ maxline }}"
               translate="{{ translate }}"
               textid="{{ textid }}"
               halign="{{ halign }}"
               valign="{{ valign }}"
               z-index="{{ z }}"
               ScriptEvents="{{ events }}"
               action="{{ action }}"
               url="{{ url }}"
               manialink="{{ manialink }}"
               focusareacolor1="{{ focusareacolor1 }}"
               focusareacolor2="{{ focusareacolor2 }}"
        />
    </template>
</component>
