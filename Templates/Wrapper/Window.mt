<component>
    <import src="Includes/WindowTitleBar.mt"/>

    <property type="int" name="z"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title"/>
    <property type="double" name="titleBarHeight" default="6.0"/>

    <template>
        <frame size="{{ w }} {{ h }}">
            <WindowTitleBar w="{{ w }}" h="{{ titleBarHeight }}" title="{{ title }}"/>
            <Frame y="{{ -titleBarHeight }}" w="{{ w }}" h="{{ h - titleBarHeight }}" z="{{ z }}">
                <slot/>
            </Frame>
        </frame>
    </template>
</component>
