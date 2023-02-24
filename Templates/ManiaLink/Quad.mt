<component>
    <property type="string" name="halign" default="left"/>
    <property type="string" name="valign" default="center"/>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w" default="0.0"/>
    <property type="double" name="h" default="0.0"/>
    <property type="double" name="opacity" default="1.0"/>
    <property type="int" name="zIndex" default="0"/>
    <property type="int" name="events" default="0"/>
    <property type="string" name="action" default=""/>
    <property type="string" name="url" default=""/>
    <property type="string" name="manialink" default=""/>
    
    <property type="string" name="image" default="" />
    <property type="string" name="imagefocus" default="" />
    <property type="string" name="style" default=""/>
    <property type="string" name="substyle" default=""/>
    <property type="string" name="styleselected" default=""/>
    <property type="string" name="colorize" default=""/>
    <property type="string" name="modulatecolor" default=""/>
    <property type="int"    name="autoscale" default="0"/>
    <property type="string" name="keepratio" default="Inactive"/>

    <template>
        <quad pos="{{ x }} {{ y }}"
              size="{{ w }} {{ h }}"
              halign="{{ halign }}"
              valign="{{ valign }}"
              opacity="{{ opacity }}"
              z-index="{{ zIndex }}"
              ScriptEvents="{{ events }}"
              action="{{ action }}"
              url="{{ url }}"
              manialink="{{ manialink }}"
              image="{{ image }}"
              imagefocus="{{ imagefocus }}"
              style="{{ style }}"
              substyle="{{ substyle }}"
              styleselected="{{ styleselected }}"
              colorize="{{ colorize }}"
              modulatecolor="{{ modulatecolor }}"
              autoscale="{{ autoscale }}"
              keepratio="{{ keepratio }}"
        />
    </template>
</component>
