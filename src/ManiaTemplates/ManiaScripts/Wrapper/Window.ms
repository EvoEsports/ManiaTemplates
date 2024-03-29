﻿#Include "TextLib" as TextLib
#Include "MathLib" as MathLib
#Include "AnimLib" as AnimLib
#Include "ColorLib" as ColorLib

//import "global" methods below

//place component methods below

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
