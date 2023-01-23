<component>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="int" name="z" default="0" />
    <property type="string" name="title" default="$iMissing title"/>
    <property type="string" name="color" default="333"/>

    <template>
        <RoundedQuad x="{{ 2.0 }}" y="{{ 2.0 * -1.0 }}" w="{{ w - 4.0 }}" h="{{ h - 4.0 }}" r="{{ 0.05 }}" color="{{ color }}" />
        <Label text="{{ title }}" x="{{ 4.0 }}" y="{{ h / -2.0 + 1.25 }}" valign="center" z="{{ z + 2 }}" textfont="GameFontBlack" textsize="{{ 1.2 }}"/>
        <Label text="✖"
               x="{{ w - h / 2 }}"
               y="{{ h / -2 }}"
               w="{{ h }}"
               h="{{ h }}"
               z="{{ z + 2 }}"
               halign="center"
               valign="center"
        />
    </template>
</component>
