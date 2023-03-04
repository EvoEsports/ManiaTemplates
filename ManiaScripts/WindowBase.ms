<script><![CDATA[
#Include "TextLib" as TextLib
#Include "MathLib" as MathLib
#Include "AnimLib" as AnimLib
#Include "ColorLib" as ColorLib

//import "global" methods here
]]></script>

//place component methods here

<script><![CDATA[
Void _nothing() {
}

/* --------------------- Main Script ------------------------ */

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