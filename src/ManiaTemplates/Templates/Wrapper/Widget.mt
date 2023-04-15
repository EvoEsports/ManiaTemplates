<component>
    <property type="int" name="zIndex"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>

    <template>
        <RoundedQuad w="{{ w }}" h="{{ h }}" radius="{{ 0.1 }}" color="111" zIndex="{{ 1 * -1 }}" opacity="{{ 0.8 }}" />
    </template>
</component>
