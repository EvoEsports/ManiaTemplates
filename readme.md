# ManiaTemplates

A XML based template engine to use for creating UI elements in the game Trackmania.
Components are used to build complex UIs with the limited set of UI elements the game provides.
This template engine eases the creation of UIs by providing loops, conditional access and properties that data can be
passed to.

Used in [EvoSC#](https://github.com/EvoEsports/EvoSC-sharp), our new server controller.
We're using ``.mt`` file extension to discriminate ManiaTemplate from other XML files,
using `.xml` would work just as well.

## Components

Components are reusable UI elements with optional properties and at least a template that defines the markup.
Any component can be included in another one, or rendered individually.

### Example component

The XML below shows an example component importing namespaces and other components, as well as defining two properties,
a template with conditional rendering and loops.

````xml

<component>
    <!-- import namespaces -->
    <using namespace="System.Linq"/>

    <!-- import other components -->
    <import component="DescriptionBox" as="DescriptionBox"/>
    <import component="Window" as="Wrapper"/>

    <!-- define properties -->
    <property name="title" type="string"/>
    <property name="description" type="string" default="No descripption available."/>

    <!-- create the markup -->
    <template>
        <Wrapper foreach="int i in Enumerable.Range(1, 3)">
            <label if="i > 1" text="{{ title + i }}"/>
            <DescriptionBox description="{{ description }}"/>
        </Wrapper>
    </template>
</component>
````

### How it works

Before rendering a template, all components are combined into a
single [T4 file](https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates), which
then is pre-compiled into a C# class.
Pre-compiling to C# class allows very fast rendering (1-3ms).

````mermaid
flowchart LR
    components(component1.mt
    component2.mt
component3.mt) -->|combine & convert|t4(template.t4)
t4 -->|pre compile|cs(template.cs)
cs -->|render|out["`*XML string*`"]
````

### List of included components

[List of global components](components.md)

## Code example

How to add component files and render a template.

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

## Setup Rider to recognize mt-files

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

## How to add embedded resources

You can use different sources, but in general we'll use embedded resources for our templates.

### In Rider

Right-click a file, go to **Properties**, then under **Build Action** select *Embedded Resource*.

To add a whole directory, add the following to the *.csproj* file:

````xml

<ItemGroup>
    <None Remove="Templates\**\*"/>
    <EmbeddedResource Include="Templates\**\*"/>
</ItemGroup>
````