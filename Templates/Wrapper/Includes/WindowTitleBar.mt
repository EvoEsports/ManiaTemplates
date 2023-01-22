<component>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title" default="$iMissing title"/>
    <property type="string" name="color" default="333"/>

    <template>
        <RoundedQuad x="{{ 1.0 }}" y="{{ 3.0 }}" w="{{ w - 2.0 }}" h="{{ h - 2.0 }}" r="{{ 0.05 }}" color="{{ color }}" />
        <Label text="{{ title }}" x="2" y="{{ h / -2.0 }}" valign="center"/>
        <Label text="close"
               x="{{ w - h / 2 }}"
               y="{{ h / -2 }}"
               w="{{ h }}"
               h="{{ h }}"
               halign="center"
               valign="center"
        />
    </template>
</component>
