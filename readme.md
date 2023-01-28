# ManiaTemplates

A templating engine to use for ManiaLinks, an XML based markup language for the game Trackmania.

## Setup Rider to support mt-files

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

## Components
[List of available components](components.md)

## Example

#### Component template (Test.xml)

````xml

<component>
    <using namespace="Models"/>
    <import src="MapRow.mt"/>

    <property type="List<Map>" name="maps"/>

    <template>
        <Window title="Map list" x="0" y="40" w="120" h="90">
            <frame foreach="var map in maps" pos="0 {{ __index * -5 }}">
                <MapRow map="{{ map }}"/>
            </frame>
        </Window>
    </template>
</component>
````

#### Rendering

````csharp
dynamic GetMapListData()
{
    var author = new Player { Login = Helper.RandomString(), Name = "AuthorOne" };
    var authorTwo = new Player { Login = Helper.RandomString(), Name = "AuthorTwo" };

    return new
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
    };
}

//Prepare
var engine = new ManiaTemplateEngine();
var mapList = engine.PreProcess(Component.FromFile("MapList/MapList.mt"));

//Render (~5ms)
var result = mapList.Render(GetMapListData());
Console.WriteLine(Helper.PrettyXml(result));
````

#### Output
````xml

<manialink version="3">
    <frame pos="0 40" size="120 90">
        <label text="Map list" pos="4 -3.25" textsize="1.2" textfont="GameFontBlack" z-index="2"/>
        <label text="?" pos="115.5 -4.5" textsize="1" textfont="GameFont" z-index="2"/>
        <frame pos="2 -9" size="116 77" z-index="1">
            <frame pos="0 0">
                <label text="F7ZYRK1VCSMNZFVI" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap1" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorOne" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
            <frame pos="0 -5">
                <label text="JPD07D944RQQH9IK" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap2" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorOne" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
            <frame pos="0 -10">
                <label text="FUQ0XTCKOUMOR4WQ" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap3" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorTwo" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
            <frame pos="0 -15">
                <label text="VH10FT8X4NHI282K" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap4" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorOne" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
            <frame pos="0 -20">
                <label text="OEUJH5ZSAO1EMB1X" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap5" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorTwo" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
            <frame pos="0 -25">
                <label text="V36MM51JUS83G1P1" pos="0 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="TestMap6" pos="40 0" textsize="1" textfont="GameFont" z-index="0"/>
                <label text="AuthorTwo" pos="80 0" textsize="1" textfont="GameFont" z-index="0"/>
            </frame>
        </frame>
    </frame>
</manialink>
````