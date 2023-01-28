<component>
    <import src="Includes/WindowTitleBar.mt"/>

    <property type="int" name="z"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title"/>
    <property type="double" name="titleBarHeight" default="11.0"/>

    <template>
        <frame pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}">
            <RoundedQuad w="{{ w }}" h="{{ h }}" r="{{ 0.1 }}" color="111" z="{{ -1 }}" o="{{ 0.8 }}" />
            <WindowTitleBar w="{{ w }}" h="{{ titleBarHeight }}" color="f06" title="{{ title }}"/>
            <Frame x="{{ 2.0 }}" y="{{ titleBarHeight * -1.0 - 1.0 }}" w="{{ w - 4.0 }}" h="{{ h - titleBarHeight - 4.0 }}" z="{{ z + 1 }}">
                <slot/>
            </Frame>
        </frame>
    </template>
</component>
