<component>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="int" name="zIndex" default="0"/>

    <template>
        <frame pos="{{ x }} {{ y }}" z-index="{{ zIndex }}">
            <!-- TODO -->
            <RoundedQuad size="4 4" />
        </frame>
    </template>
</component>
