<component>
    <property type="int" name="z-index"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>

    <template>
        <RoundedQuad w="{{ w }}" h="{{ h }}" radius="{{ 0.1 }}" color="111" z-index="{{ 1 * -1 }}" opacity="{{ 0.8 }}" />
    </template>
</component>
