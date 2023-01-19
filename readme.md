# ManiaTemplates

A templating engine to use for ManiaLinks, an XML based markup language for the game Trackmania.

## Setup Rider to support mt-files

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

## Example

#### Component template (Test.xml)

````xml
<component>
    <property type="string" name="title"/>
    <property type="double" name="w"/>
    <property type="double" name="h"/>

    <template>
        <Label x="20.0" y="{{ 11 * 4.0 }}" text="{{ title }}_append_test"/>

        <Window w="{{ w }}" h="{{ h }}" title="{{ title }}" z="123">
            <Label text="Hey there!"/>
        </Window>
    </template>
</component>
````

#### Rendering

````csharp
using ManiaTemplates;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;

//Initialize engine
var engine = new ManiaTemplateEngine(new T4());

//Load a component
var testComponent = Component.FromFile("Templates/Test.xml");
var manialink = engine.ConvertComponent(testComponent);

//Render it
var result = engine.Render(manialink, new
{
    title = "Custom Window Title",
    w = 120.0,
    h = 90.0
});

//Print it
Console.WriteLine($"\n{Helper.PrettyXxml(result.Result)}");
````

#### Output

````xml
<manialink version="3">
    <label text="Custom Window Title_append_test" pos="20 44" textsize="1" />
    <frame size="120 90">
        <quad pos="0 0" size="120 6" bgcolor="333" />
        <label text="Custom Window Title" pos="2 -3" textsize="1" />
        <label text="close" pos="117 -3" textsize="1" />
        <frame pos="0 -6" size="120 84" z-index="123">
            <label text="Hey there!" pos="0 0" textsize="1" />
        </frame>
    </frame>
</manialink>
````