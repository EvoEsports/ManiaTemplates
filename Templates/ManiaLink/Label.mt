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
    <property type="string" name="action" default="" />
    <property type="string" name="url" default="" />
    <property type="string" name="manialink" default="" />

    <property type="string" name="style" default=""/>
    <property type="string" name="textfont" default="GameFont"/>
    <property type="double" name="textsize" default="1.0"/>
    <property type="string" name="textcolor" default="" />
    <property type="string" name="focusareacolor1" default="" />
    <property type="string" name="focusareacolor2" default="" />

    <property type="string" name="text" default=""/>
    <property type="string" name="textprefix" default=""/>
    <property type="int" name="bold" default="0"/>
    <property type="int" name="autonewline" default="0"/>
    <property type="int" name="maxline" default="0"/>
    <property type="int" name="translate" default="0"/>
    <property type="string" name="textid" default=""/>

    <template>
        <label text="{{ text }}"
               textprefix="{{ textprefix }}"
               style="{{ style }}"
               pos="{{ x }} {{ y }}"
               size="{{ w }} {{ h }}"
               textsize="{{ textsize }}"
               textfont="{{ textfont }}"
               textcolor="{{ textcolor }}"
               textemboss="{{ bold }}"
               autonewline="{{ autonewline }}"
               maxline="{{ maxline }}"
               translate="{{ translate }}"
               textid="{{ textid }}"
               halign="{{ halign }}"
               valign="{{ valign }}"
               opacity="{{ opacity }}"
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
