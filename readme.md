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

### Loops and conditions

Any node, including components, can be repeated or rendered conditionally using the following attributes.

| Attribute | Example                             | Description                                                                 |
|-----------|-------------------------------------|-----------------------------------------------------------------------------|
| `foreach` | `foreach="int i in numbers"`        | Renders the node for each element. The type of the variables is required.   |
| `for`     | `for="int i = 0; i < count; i++"`   | Renders the node like a C# for loop. The type of the variables is required. |
| `if`      | `if="i > 1"`                        | Only renders the node if the condition is true.                             |
| `else-if` | `else-if="i == 1"`                  | Continues the condition chain of the preceding node.                        |
| `else`    | `else=""`                           | Renders the node if no condition of the chain was true.                     |

Inside loops the zero-based iteration index is available as `__index`, nested loops use `__index2`, `__index3` and so
on.
Nested loops must declare variables with different names than their surrounding loops.

`else-if` and `else` nodes must follow a node with an `if` or `else-if` attribute, only comments may be in between.

A node can either have a loop or a condition, to make the evaluation order explicit, nest them instead.
To do so without adding an element to the output, use a [fragment](#fragments):

````xml
<label if="maps.Count == 0" text="No maps found."/>
<!-- List all maps otherwise -->
<template else="">
    <label foreach="Map map in maps" text="{{ map.Name }}"/>
</template>
````

### Fragments

A `<template>` node inside a template is a fragment: it only renders its children, without producing an element itself.
Use it to group nodes for loops and conditions:

````xml
<template foreach="Player player in players">
    <label text="{{ player.Name }}"/>
    <label text="{{ player.Score }}"/>
</template>
````

As fragments have no element of their own, they only support the loop and condition attributes, any other attribute
throws an exception.
A component whose template consists of a single fragment does not receive fallthrough attributes, as it may render
multiple root elements.

### Slots

Components can define slots using `<slot/>` nodes, which are filled with content when using the component.
Besides the unnamed default slot, a component can define named slots:

````xml
<!-- Panel.mt -->
<component>
    <template>
        <frame>
            <slot/>
            <frame pos="0 -50">
                <slot name="footer"/>
            </frame>
        </frame>
    </template>
</component>
````

All children of a component node fill the default slot.
Named slots are filled using `<template slot="name">` nodes, which must be direct children of the component node.
Each slot can only be filled by one template, templates for slots the component does not define are ignored.

Slots and the templates filling them do not support loops and conditions, place them inside the template or use a
[fragment](#fragments) around the slot instead:

````xml
<Panel>
    <label text="Rendered in the default slot"/>

    <template slot="footer">
        <label if="showFooter" text="Rendered in the footer slot"/>
        <label else="" text="Rendered otherwise"/>
    </template>
</Panel>
````

Slot contents can use the properties and loop variables available where the component is used.

### How it works

Before rendering a template, all components are combined into a
single [T4 file](https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates), which
then is pre-compiled into a C# class.
Pre-compiling to C# class allows very fast rendering (1-3ms).

````mermaid
flowchart LR
    components(component1.mt, \ncomponent2.mt, \ncomponent3.mt) -->|combine & convert|t4(template.t4)
    t4 -->|pre compile|cs(template.cs)
    cs -->|render|out[XML string]
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
