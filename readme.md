# ManiaTemplates

A templating engine to use for ManiaLinks, an XML based markup language for the game Trackmania.

## Setup Rider to support mt-files

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

## Example

#### Component template (Test.xml)

````xml
<component>
    <import src="MapRow.mt" />

    <property type="List<Map>" name="maps" default="null" />

    <template>
        <Window title="Map list" x="10" w="{{ 120 }}" h="{{ 90 }}">
            <MapRow foreach="var map in maps" y="{{ __index * -6 }}" map="{{ map }}" />
        </Window>
    </template>
</component>

````

#### Rendering

````csharp
// 1. Compile the template
new ManiaTemplateEngine(new T4()).PreProcess(
    Component.FromFile("MapList.mt"),
    "MapList"
);

// 2. Render the template
var result = new MapList
{
    maps = new List<Map>
    {
        new() { Uid = Helper.RandomString(), Name = "TestMap1", Author = author },
        new() { Uid = Helper.RandomString(), Name = "TestMap2", Author = author },
        new() { Uid = Helper.RandomString(), Name = "TestMap3", Author = authorTwo },
        new() { Uid = Helper.RandomString(), Name = "TestMap4", Author = author },
        new() { Uid = Helper.RandomString(), Name = "TestMap5", Author = authorTwo },
        new() { Uid = Helper.RandomString(), Name = "TestMap6", Author = authorTwo },
    }
}.TransformText();
````

#### Output
````xml
<manialink version="3">
  <frame pos="10 0" size="120 90">
    <quad pos="0 0" size="120 6" bgcolor="333" />
    <label text="Map list" pos="2 -3" textsize="1" />
    <label text="close" pos="117 -3" textsize="1" />
    <frame pos="0 6" size="120 84" z-index="0">
      <label text="WX070D2TKX94HU0H" pos="0 0" textsize="1" />
      <label text="TestMap1" pos="40 0" textsize="1" />
      <label text="AuthorOne" pos="80 0" textsize="1" />
      <label text="N471T748PKRSSCYQ" pos="0 -6" textsize="1" />
      <label text="TestMap2" pos="40 -6" textsize="1" />
      <label text="AuthorOne" pos="80 -6" textsize="1" />
      <label text="0Z3JO3XRSMCBI25F" pos="0 -12" textsize="1" />
      <label text="TestMap3" pos="40 -12" textsize="1" />
      <label text="AuthorTwo" pos="80 -12" textsize="1" />
      <label text="8QHV4TMUILV59H00" pos="0 -18" textsize="1" />
      <label text="TestMap4" pos="40 -18" textsize="1" />
      <label text="AuthorOne" pos="80 -18" textsize="1" />
      <label text="XTKEPELQZ5LOBQMB" pos="0 -24" textsize="1" />
      <label text="TestMap5" pos="40 -24" textsize="1" />
      <label text="AuthorTwo" pos="80 -24" textsize="1" />
      <label text="2NDUGKPCM18AD616" pos="0 -30" textsize="1" />
      <label text="TestMap6" pos="40 -30" textsize="1" />
      <label text="AuthorTwo" pos="80 -30" textsize="1" />
    </frame>
  </frame>
</manialink>
````