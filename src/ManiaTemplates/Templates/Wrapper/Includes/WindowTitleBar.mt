﻿<component>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="int" name="zIndex" default="0" />
    <property type="string" name="title" default="$iMissing title"/>
    <property type="string" name="color" default="333"/>

    <template>
        <RoundedQuad x="{{ 2.0 }}" y="{{ 2.0 * -1.0 }}" w="{{ w - (h - 4.0) - 5.0 }}" h="{{ h - 4.0 }}" radius="{{ 0.05 }}" color="{{ color }}" tr="false" br="false" />
        <Label text="{{ title }}" x="{{ 4.0 }}" y="{{ h / -2.0 + 1.25 }}" valign="center" zIndex="{{ zIndex + 2 }}" textfont="GameFontBlack" textsize="{{ 1.4 }}"/>
        
        <RoundedQuad x="{{ w - (h - 4.0) - 2.0 }}" y="{{ 2.0 * -1.0 }}" w="{{ h - 4.0 }}" h="{{ h - 4.0 }}" radius="{{ 0.05 }}" color="{{ color }}" tl="false" bl="false" />
        <Label text="x"
               x="{{ w - ((h - 4.0) / 2.0) - 3.0 }}"
               y="{{ h / -2.0 + 1.25 }}"
               zIndex="{{ zIndex + 2 }}"
               halign="center"
               valign="center"
               textfont="GameFontBlack"
               textsize="{{ 1.4 }}"
               id="window_close_button"
        />
        
        <script><!--
            *** OnMouseClick ***
            ***
            if(Event.Control && Event.Control.ControlId == "window_close_button"){
                Page.MainFrame.Hide();
                return;
            }
            ***
        --></script>
    </template>
</component>
