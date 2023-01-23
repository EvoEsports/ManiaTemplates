<component>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w" default="0.0"/>
    <property type="double" name="h" default="0.0"/>
    <property type="double" name="r" default="0.5"/>
    <property type="double" name="o" default="1.0"/>
    <property type="int" name="z" default="1"/>
    <property type="string" name="color" default="FFF"/>

    <!-- file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga -->

    <template>
        <frame pos="{{ x }} {{ y }}" z-index="{{ z }}">
            <frame pos="{{ w }} 0" size="10 10" rot="90" scale="{{ r }}">
                <quad pos="-.15 0.15" size="20 20" modulatecolor="{{ color }}" image="file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga" opacity="{{ o }}"/>
            </frame>
            <frame pos="{{ w }} {{ h * -1.0 }}" size="10 10" rot="180" scale="{{ r }}">
                <quad pos="-.15 0.15" size="20 20" modulatecolor="{{ color }}" image="file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga" opacity="{{ o }}"/>
            </frame>
            <frame pos="0 {{ h * -1.0 }}" size="10 10" rot="-90" scale="{{ r }}">
                <quad pos="-.15 0.15" size="20 20" modulatecolor="{{ color }}" image="file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga" opacity="{{ o }}"/>
            </frame>
            <frame pos="0 0" size="10 10" scale="{{ r }}">
                <quad pos="-.15 0.15" size="20 20" modulatecolor="{{ color }}" image="file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga" opacity="{{ o }}"/>
            </frame>
            <quad pos="{{ r * 10.0 }} 0" size="{{ w - r * 20.0 }} {{ r * 10.0 }}" bgcolor="{{ color }}" opacity="{{ o }}" />
            <quad pos="0 {{ r * -10.0 }}" size="{{ w }} {{ h - r * 20.0 }}" bgcolor="{{ color }}" opacity="{{ o }}" />
            <quad pos="{{ r * 10.0 }} {{ h * -1.0 + r * 10.0 }}" size="{{ w - r * 20.0 }} {{ r * 10.0 }}" bgcolor="{{ color }}" halign="bottom" opacity="{{ o }}" />
        </frame>
    </template>
</component>
