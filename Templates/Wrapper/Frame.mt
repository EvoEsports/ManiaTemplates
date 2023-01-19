<component>
    <property type="int" name="z"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>

    <template>
        <frame pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}" z-index="{{ z }}">
            <slot/>
        </frame>
    </template>
</component>
