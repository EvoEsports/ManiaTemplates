# ManiaTemplates

A XML based template engine to use for creating UI elements in the game Trackmania.
Components are used to build complex UIs with the limited set of UI elements the game provides.
This template engine eases the creation of UIs by providing loops, conditional access and properties that data can be
passed to.

Used in [EvoSC#](https://github.com/EvoEsports/EvoSC-sharp), our new server controller.
We're using ``.mt`` file extension to discriminate ManiaTemplate from other XML files,
using `.xml` would work just as well.

* [Getting started](#getting-started)
* [Components](#components)
* [Writing templates](#writing-templates)
* [Slots](#slots)
* [ManiaScript](#maniascript)
* [How it works](#how-it-works)
* [Editor setup](#editor-setup)

## Getting started

Register your templates with a `ManiaTemplateEngine` under a key of your choice. To render one, call `RenderAsync` with
its key and the values for its properties:

````csharp
var engine = new ManiaTemplateEngine();

//Register the templates when your module loads, the key is used to import and render them
engine.AddTemplateFromString("MapRow", mapRowTemplate);
engine.AddTemplateFromString("MapList", mapListTemplate);

//Assemblies containing the types your templates use
var assemblies = new[] { typeof(Map).Assembly };

//Optional: pre-process the template, so the first render is fast
await engine.PreProcessAsync("MapList", assemblies);

//Render the manialink as an XML string, the data fills the properties of the template
var xml = await engine.RenderAsync("MapList", new { maps = GetMaps(), title = "Maps" }, assemblies);
````

The data can also be an `IDictionary<string, object?>`.
Templates embedded in your assembly can be added using `LoadTemplateFromEmbeddedResource`, see
[embedded resources](#embedded-resources).

Values that every template needs, like the server name, can be set once as global variables:

````csharp
engine.GlobalVariables["serverName"] = "My server";
````

````xml
<label text="{{ serverName }}"/>
````

## Components

Components are reusable UI elements. Each has a template that defines its markup, and optionally properties that
receive data. A component can be imported by other components, or rendered on its own.

````xml
<component>
    <!-- import namespaces -->
    <using namespace="System.Linq"/>

    <!-- import other components, the key is the one the template was added with -->
    <import component="DescriptionBox" as="DescriptionBox"/>
    <import component="Window" as="Wrapper"/>

    <!-- define properties -->
    <property name="title" type="string"/>
    <property name="description" type="string" default="No description available."/>

    <!-- create the markup -->
    <template>
        <Wrapper foreach="int i in Enumerable.Range(1, 3)">
            <label if="i > 1" text="{{ title + i }}"/>
            <DescriptionBox description="{{ description }}"/>
        </Wrapper>
    </template>
</component>
````

| Node         | Description                                                                                |
|--------------|--------------------------------------------------------------------------------------------|
| `<using>`    | Imports a C# namespace, so its types can be used in expressions and property types.        |
| `<import>`   | Makes a component available as a node with the name given by `as`.                         |
| `<property>` | Declares a value the component receives. Properties without a `default` are required.     |
| `<template>` | The markup of the component.                                                               |
| `<script>`   | Adds [ManiaScript](#maniascript) to the manialink.                                         |

A component is used like any other node, its properties are set using attributes:

````xml
<DescriptionBox description="Shown in the box"/>
````

### Expressions

Attribute values can contain C# expressions inside `{{ }}`. You can use the properties of the component, the variables
of surrounding loops and [global variables](#getting-started).

````xml
<label text="{{ player.Name }}" size="{{ width * 2 }} 5"/>
<DescriptionBox description="Score: {{ score }}"/>
````

Properties of type `string` can mix text and expressions, like `description="Score: {{ score }}"`.
Properties of other types take a single expression, like `width="{{ 12.5 }}"`.

### Fallthrough attributes

Attributes that are not properties of the component are passed on to its root element, provided the template has only
one. This lets you use regular attributes like `pos` or `size` on your components:

````xml
<Panel pos="10 20" data-id="{{ id }}"/>
````

Components with multiple root elements, or a [fragment](#fragments) as root, ignore these attributes.

## Writing templates

### Loops and conditions

Any node, including components, can be repeated or rendered conditionally using these attributes.

| Attribute | Example                           | Description                                                                 |
|-----------|-----------------------------------|-----------------------------------------------------------------------------|
| `foreach` | `foreach="int i in numbers"`      | Renders the node for each element. The type of the variables is required.   |
| `for`     | `for="int i = 0; i < count; i++"` | Renders the node like a C# for loop. The type of the variables is required. |
| `if`      | `if="i > 1"`                      | Only renders the node if the condition is true.                             |
| `else-if` | `else-if="i == 1"`                | Continues the condition chain of the preceding node.                        |
| `else`    | `else=""`                         | Renders the node if no condition of the chain was true.                     |

Inside loops the zero-based iteration index is available as `__index`, nested loops use `__index2`, `__index3` and so
on. Nested loops must declare variables with different names than their surrounding loops.

`else-if` and `else` nodes must follow a node with an `if` or `else-if` attribute, only comments may be in between.

A node can have either a loop or a condition. To combine them, nest the nodes using a [fragment](#fragments):

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

Fragments only support loop and condition attributes, any other attribute causes an exception.

## Slots

Slots let whoever uses a component decide what is rendered inside it, like the content of a window.
A component defines slots using `<slot/>` nodes. Besides the unnamed default slot, it can define named slots:

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

### Filling slots

All children of a component node fill the default slot.
Named slots are filled using `<template slot="name">` nodes, which must be direct children of the component node:

````xml
<Panel>
    <label text="Rendered in the default slot"/>

    <template slot="footer">
        <label if="showFooter" text="Rendered in the footer slot"/>
        <label else="" text="Rendered otherwise"/>
    </template>
</Panel>
````

* Each slot can only be filled by one template.
* Templates for slots the component does not define are ignored.
* `<template slot>` itself does not support `if` and loops, use them on the contents inside of it.
* The contents can use the variables available where you use the component, like its properties and loop variables.

### Slot arguments

A component can pass values to the contents of its slots. They are available in the contents as variables, named after
the attributes of the `<slot>`:

````xml
<!-- Usage -->
<ItemList items="{{ names }}">
    <label text="{{ item }} at {{ index }}"/>
</ItemList>
````

Here `ItemList` passes `item` and `index` to its slot. The documentation of a component lists the arguments it passes.
Don't give your own variables the same name as a passed argument, this is rejected.

### Defining slots

Slots support all conditions (`if`, `else-if`, `else`) and loops (`for`, `foreach`) to render them conditionally or
repeatedly. A slot can also be placed at multiple positions, its contents are rendered at each of them:

````xml
<!-- ItemList.mt -->
<component>
    <property type="List<string>" name="items"/>
    <property type="bool" name="showFooter"/>
    <template>
        <template foreach="string item in items">
            <slot item="{{ item }}" index="{{ __index }}"/>
        </template>
        <slot name="footer" if="showFooter"/>
    </template>
</component>
````

Arguments are attributes of the `<slot>`. Their value must be a single variable available at the slot, like a loop
variable or a property of the component, written like `item="{{ item }}"`. A slot rendered at multiple positions must
pass the same arguments at each of them.

## ManiaScript

Components can include ManiaScript using `<script>` nodes, either inline or from a script registered with
`engine.AddManiaScriptFromString("key", script)`:

````xml
<component>
    <template>
        <label id="status" text="Loading"/>
    </template>

    <script>
        main() {
            // ...
        }
    </script>
    <script resource="Shared.Helpers" once="true"/>
</component>
````

With the `once` attribute the script is added only a single time, even if the component is used multiple times.

## How it works

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

Errors in expressions, like an unknown variable, are reported when the template is compiled. This happens on the first
render, or when calling `PreProcessAsync`.

## Included components

[List of global components](components.md)

## Editor setup

### Recognize mt-files in Rider

* Go to **Editor > File Types**, select *XML* in the recognized file types list.
* Add *.mt* on the right side.

### Embedded resources

Templates can come from any source, but we usually embed them as resources.
In Rider, right-click a file, go to **Properties** and select *Embedded Resource* as **Build Action**.

To add a whole directory, add the following to the *.csproj* file:

````xml
<ItemGroup>
    <None Remove="Templates\**\*"/>
    <EmbeddedResource Include="Templates\**\*"/>
</ItemGroup>
````

Add embedded files to the engine using their resource path, which also serves as their key:

````csharp
engine.LoadTemplateFromEmbeddedResource("MyModule.Templates.MapRow.mt");
````
