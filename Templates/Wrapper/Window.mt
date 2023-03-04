<component>
    <import resource="ManiaTemplates.Templates.Wrapper.Includes.WindowTitleBar.mt"/>

    <property type="int" name="zIndex"/>
    <property type="double" name="x"/>
    <property type="double" name="y"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>
    <property type="string" name="title"/>
    <property type="double" name="titleBarHeight" default="11.0"/>

    <template>
        <frame pos="{{ x }} {{ y }}" size="{{ w }} {{ h }}">
            <RoundedQuad w="{{ w }}" h="{{ h }}" radius="{{ 0.1 }}" color="111" zIndex="{{ -1 }}" opacity="{{ 0.8 }}"/>
            <WindowTitleBar w="{{ w }}" h="{{ titleBarHeight }}" color="f06" title="{{ title }}"/>
            <Frame x="{{ 2.0 }}" y="{{ titleBarHeight * -1.0 - 1.0 }}" w="{{ w - 4.0 }}"
                   h="{{ h - titleBarHeight - 4.0 }}" zIndex="{{ zIndex + 1 }}">
                <slot/>
            </Frame>
        </frame>
    </template>

    <script resource="ManiaTemplates.ManiaScripts.WindowBase.ms"><![CDATA[
#Include "TextLib" as TextLib
#Include "MathLib" as MathLib
#Include "AnimLib" as AnimLib
#Include "ColorLib" as ColorLib

Void _nothing() {
}

main() {
  +++OnInit+++

  while(True) {
    yield;
    if (!PageIsVisible || InputPlayer == Null) {
  			continue;
  	}

    foreach (Event in PendingEvents) {
			switch (Event.Type) {
				case CMlScriptEvent::Type::EntrySubmit: {
					+++EntrySubmit+++
				}
				case CMlScriptEvent::Type::KeyPress: {
					+++OnKeyPress+++
				}
				case CMlScriptEvent::Type::MouseClick: {
					+++OnMouseClick+++
				}
				case CMlScriptEvent::Type::MouseOut: {
					+++OnMouseOut+++
				}
				case CMlScriptEvent::Type::MouseOver: {
					+++OnMouseOver+++
				}
			}
		}

		+++Loop+++
  }

}
]]></script>
</component>
