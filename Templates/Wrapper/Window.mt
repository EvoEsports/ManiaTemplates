<component>
    <import resource="ManiaTemplates.Templates.Wrapper.Includes.WindowTitleBar.mt" relative="true"/>

    <property type="int" name="zIndex"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title"/>
    <property type="double" name="titleBarHeight" default="11.0"/>

    <template>
        <frame pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}">
            <RoundedQuad w="{{ w }}" h="{{ h }}" r="{{ 0.1 }}" color="111" z-index="{{ -1 }}" o="{{ 0.8 }}" />
            <WindowTitleBar w="{{ w }}" h="{{ titleBarHeight }}" color="f06" title="{{ title }}"/>
            <Frame x="{{ 2.0 }}" y="{{ titleBarHeight * -1.0 - 1.0 }}" w="{{ w - 4.0 }}" h="{{ h - titleBarHeight - 4.0 }}" z-index="{{ z-index + 1 }}">
                <slot/>
            </Frame>
        </frame>
    </template>
    
    <script><!--
    
    main(){
        log("Window opened.");
    }
    
    --></script>
</component>
