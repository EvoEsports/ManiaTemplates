<component>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="int" name="z-index" default="0"/>

    <template>
        <frame pos="{{ x }} {{ y }}" z-index="{{ z-index }}">
            <!-- TODO -->
            <RoundedQuad size="4 4" />
        </frame>
    </template>
</component>
