﻿<component>
    <property type="int" name="zIndex" default="0"/>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>

    <template>
        <frame pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}" z-index="{{ zIndex }}">
            <slot/>
        </frame>
    </template>
</component>
