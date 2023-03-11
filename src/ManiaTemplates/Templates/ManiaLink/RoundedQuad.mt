<component>
    <property type="double" name="x" default="0.0"/>
    <property type="double" name="y" default="0.0"/>
    <property type="double" name="w" default="0.0"/>
    <property type="double" name="h" default="0.0"/>
    <property type="double" name="radius" default="0.5"/>
    <property type="double" name="opacity" default="1.0"/>
    <property type="bool" name="tr" default="true"/>
    <property type="bool" name="br" default="true"/>
    <property type="bool" name="bl" default="true"/>
    <property type="bool" name="tl" default="true"/>
    <property type="int" name="zIndex" default="1"/>
    <property type="string" name="color" default="FFF"/>

    <template>
        <frame pos="{{ x }} {{ y }}" z-index="{{ zIndex }}">
            <framemodel id="EvoSC_RQTB_{{ color + opacity + w + radius }}">
                <quad size="{{ w - radius * 20.0 }} {{ radius * 10.0 }}"
                      bgcolor="{{ color }}"
                      opacity="{{ opacity }}"/>
            </framemodel>
            <framemodel id="EvoSC_RQC_{{ color + opacity }}">
                <quad pos="-.15 0.15"
                      size="20 20"
                      modulatecolor="{{ color }}"
                      image="file:///Media/Painter/Stencils/01-EllipseRound/Brush.tga"
                      opacity="{{ opacity }}"/>
            </framemodel>

            <frame if="{{ tr }}" pos="{{ w }} 0" size="10 10" rot="90" scale="{{ radius }}">
                <frameinstance modelid="EvoSC_RQC_{{ color + opacity }}"/>
            </frame>
            <frame if="{{ br }}" pos="{{ w }} {{ h * -1.0 }}" size="10 10" rot="180" scale="{{ radius }}">
                <frameinstance modelid="EvoSC_RQC_{{ color + opacity }}"/>
            </frame>
            <frame if="{{ bl }}" pos="0 {{ h * -1.0 }}" size="10 10" rot="-90" scale="{{ radius }}">
                <frameinstance modelid="EvoSC_RQC_{{ color + opacity }}"/>
            </frame>
            <frame if="{{ tl }}" pos="0 0" size="10 10" rot="0" scale="{{ radius }}">
                <frameinstance modelid="EvoSC_RQC_{{ color + opacity }}"/>
            </frame>

            <frameinstance modelid="EvoSC_RQTB_{{ color + opacity + w + radius }}" pos="{{ radius * 10.0 }} 0"/>
            <frameinstance modelid="EvoSC_RQTB_{{ color + opacity + w + radius }}" pos="{{ radius * 10.0 }} {{ h * -1.0 + radius * 10.0 }}"/>
            <quad pos="0 {{ radius * -10.0 }}" size="{{ w }} {{ h - radius * 20.0 }}" bgcolor="{{ color }}" opacity="{{ opacity }}"/>
        </frame>
    </template>
</component>
