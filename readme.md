# ManiaTemplates

A templating engine to use for ManiaLinks, an XML based markup language for the game Trackmania.

## Setup Rider to support mt-files

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

## Components
[List of global components](components.md)

## Example

````csharp
//Prepare template engine
var engine = new ManiaTemplateEngine();

//Add resources when module loads
engine.LoadTemplateFromEmbeddedResource("Tester.Templates.MapRow.mt");
var mapList = engine.LoadTemplateFromEmbeddedResource("Tester.Templates.MapList.mt");

//Optionally pre-process the template for faster first render
mapList.PreProcess();

//Render manialink
var result = mapList.RenderAsync(GetMapListData());
````

## How to add embedded resources
You can use different sources, but in general we'll use embedded resources for our templates.
### In Rider
Right-click a file, go to **Properties**, then under **Build Action** select *Embedded Resource*.

To add a whole directory, add the following to the *.csproj* file:
````xml
<ItemGroup>
  <None Remove="Templates\**\*" />
  <EmbeddedResource Include="Templates\**\*" />
</ItemGroup>
````