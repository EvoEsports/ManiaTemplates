<component>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title" default="$iMissing title"/>
    <property type="string" name="color" default="333"/>

    <template>
        <Quad w="{{ w }}" h="{{ h }}" bg="{{ color }}"/>
        <Label text="{{ title }}" x="2" y="{{ h / -2 }}"/>
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
