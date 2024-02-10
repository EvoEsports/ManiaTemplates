using System.CodeDom;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;
using Microsoft.CSharp;

namespace ManiaTemplates.Lib;

public class MtTransformer : CurlyBraceMethods
{
    private readonly ManiaTemplateEngine _engine;
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage;
    private readonly MtScriptTransformer _scriptTransformer;
    private readonly List<string> _namespaces = new();
    private readonly Dictionary<string, string> _renderMethods = new();
    private readonly Dictionary<string, string> _maniaScriptRenderMethods = new();
    private readonly List<MtComponentSlot> _slots = new();
    private int _loopDepth;

    private static readonly Regex TemplateFeatureControlRegex = new(@"#>\s*<#\+");

    public MtTransformer(ManiaTemplateEngine engine, IManiaTemplateLanguage maniaTemplateLanguage)
    {
        _engine = engine;
        _maniaTemplateLanguage = maniaTemplateLanguage;
        _scriptTransformer = new MtScriptTransformer(maniaTemplateLanguage);
    }

    /// <summary>
    /// Creates the target language template, which can be pre-processed for faster rendering.
    /// </summary>
    public string BuildManialink(MtComponent rootComponent, string className, int version = 3)
    {
        _namespaces.AddRange(rootComponent.Namespaces);

        var renderBodyArguments = string.Join(',', Enumerable.Repeat("DoNothing", rootComponent.Slots.Count));

        var body = ProcessNode(
            XmlStringToNode(rootComponent.TemplateContent),
            _engine.BaseMtComponents.Overload(rootComponent.ImportedComponents),
            new MtDataContext(),
            rootComponent,
            rootComponent
        );

        var template = new Snippet
        {
            _maniaTemplateLanguage.Context(@"template language=""C#"""), //Might not be needed
            _maniaTemplateLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            CreateImportStatements(),
            ManiaLinkStart(className, version, rootComponent.DisplayLayer),
            "<#",
            $"RenderBody({renderBodyArguments});",
            "#>",
            ManiaLinkEnd(),
            CreateTemplatePropertiesBlock(rootComponent),
            CreateInsertedManiaScriptsList(),
            CreateDoNothingMethod(),
            CreateBodyRenderMethod(body, rootComponent),
            CreateRenderMethodsBlock()
        };

        return JoinFeatureBlocks(template.ToString());
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateDoNothingMethod()
    {
        return _maniaTemplateLanguage.FeatureBlock("private static void DoNothing(){}").ToString();
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateInsertedManiaScriptsList()
    {
        return new StringBuilder()
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("private List<string> __insertedOneTimeManiaScripts = new List<string>();")
            .AppendLine("private List<Action> __maniaScriptRenderMethods = new List<Action>();")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtComponent rootComponent)
    {
        var methodArguments = new List<string>();
        AppendSlotRenderArgumentsToList(methodArguments, rootComponent);
        var bodyRenderMethod = new StringBuilder($"private void RenderBody({string.Join(',', methodArguments)}) {{\n");

        //Root mania script block
        var rootScriptBlock = "";
        if (rootComponent.Scripts.Count > 0)
        {
            rootScriptBlock = _scriptTransformer.CreateManiaScriptBlock(rootComponent);
        }

        var subManiaScripts = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine(
                "foreach(var maniaScriptRenderMethod in __maniaScriptRenderMethods){ maniaScriptRenderMethod(); }")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();

        //Render content
        bodyRenderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(_scriptTransformer.CreateManiaScriptDirectivesBlock())
            .AppendLine(body)
            .AppendLine(subManiaScripts)
            .AppendLine(rootScriptBlock)
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}");

        return _maniaTemplateLanguage.FeatureBlock(bodyRenderMethod.ToString()).ToString();
    }

    /// <summary>
    /// Takes all loaded namespaces and creates the import statements for the target language.
    /// </summary>
    private string CreateImportStatements()
    {
        var imports = new StringBuilder();

        foreach (var propertyValue in _engine.GlobalVariables.Values)
        {
            var nameSpace = propertyValue.GetType().Namespace;

            if (nameSpace != "System")
            {
                imports.AppendLine(_maniaTemplateLanguage.Context($@"import namespace=""{nameSpace}"""));
            }
        }

        foreach (var nameSpace in _namespaces)
        {
            imports.AppendLine(_maniaTemplateLanguage.Context($@"import namespace=""{nameSpace}"""));
        }

        return imports.ToString();
    }

    /// <summary>
    /// Creates a block containing the templates properties, which are filled when rendered.
    /// </summary>
    private string CreateTemplatePropertiesBlock(MtComponent mtComponent)
    {
        var properties = new StringBuilder();

        foreach (var (propertyName, propertyValue) in _engine.GlobalVariables)
        {
            var type = propertyValue.GetType();

            properties.AppendLine(_maniaTemplateLanguage
                .FeatureBlock($"public {GetFormattedName(type)} ?{propertyName} {{ get; init; }}").ToString());
        }

        foreach (var property in mtComponent.Properties.Values)
        {
            properties.AppendLine(_maniaTemplateLanguage
                .FeatureBlock(
                    $"public {property.Type} {property.Name} {{ get; init; }}{(property.Default == null ? "" : $" = {WrapIfString(property, property.Default)};")}")
                .ToString());
        }

        return properties.ToString();
    }

    /// <summary>
    /// Creates a block containing all render methods used in the template.
    /// </summary>
    private string CreateRenderMethodsBlock()
    {
        return new StringBuilder()
            .AppendJoin("\n", _renderMethods.Values)
            .AppendJoin("\n", _maniaScriptRenderMethods.Values)
            .AppendJoin("\n", _slots.Select(slot => slot.RenderMethodT4))
            .ToString();
    }

    /// <summary>
    /// Creates the slot-render method for a given data context.
    /// </summary>
    private string CreateSlotRenderMethod(MtComponent component, int scope, MtDataContext context, string slotName,
        MtComponent rootComponent, string slotContent, MtComponent parentComponent)
    {
        var methodArguments = new List<string>();
        var methodName = GetSlotRenderMethodName(scope, slotName);

        //Add slot render methods
        AppendSlotRenderArgumentsToList(methodArguments, parentComponent);

        //Add component properties as arguments
        foreach (var (localVariableName, localVariableType) in context)
        {
            if (!rootComponent.Properties.ContainsKey(localVariableName))
            {
                methodArguments.Add($"{localVariableType} {localVariableName}");
            }
        }

        if (parentComponent != rootComponent)
        {
            AppendComponentPropertiesToMethodArgumentsList(parentComponent, methodArguments);
        }

        //Start slot render method declaration
        var output = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("private void " + CreateMethodCall(methodName, string.Join(',', methodArguments), "") + " {");

        //Declare component default variables
        foreach (var prop in component.Properties.Values)
        {
            if (prop.Default == null || (parentComponent.Properties.ContainsKey(prop.Name)))
            {
                continue;
            }

            output.AppendLine($"const {prop.Type} {prop.Name} = {WrapIfString(prop, prop.Default)};");
        }

        output
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(slotContent)
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());

        return output.ToString();
    }

    /// <summary>
    /// Process a ManiaTemplate node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap availableMtComponents, MtDataContext context,
        MtComponent rootComponent, MtComponent parentComponent)
    {
        Snippet snippet = new();

        var nodeId = 1;
        foreach (XmlNode childNode in node.ChildNodes)
        {
            var subSnippet = new Snippet();
            var tag = childNode.Name;
            var attributeList = GetXmlNodeAttributes(childNode);
            var currentContext = context;

            var forEachCondition = GetForeachConditionFromNodeAttributes(attributeList, context, nodeId);
            var ifCondition = GetIfConditionFromNodeAttributes(attributeList);

            if (forEachCondition != null)
            {
                currentContext = forEachCondition.Context;
                _loopDepth++;
            }

            if (availableMtComponents.ContainsKey(tag))
            {
                //Node is a component
                var component = _engine.GetComponent(availableMtComponents[tag].TemplateKey);
                var slotContents =
                    GetSlotContentsGroupedBySlotName(childNode, component, availableMtComponents, currentContext,
                        parentComponent, rootComponent);

                var componentRenderMethodCall = ProcessComponentNode(
                    childNode.GetHashCode(),
                    component,
                    parentComponent,
                    currentContext,
                    attributeList,
                    ProcessNode(
                        XmlStringToNode(component.TemplateContent),
                        availableMtComponents.Overload(component.ImportedComponents),
                        currentContext,
                        rootComponent: rootComponent,
                        parentComponent: component
                    ),
                    slotContents,
                    rootComponent: rootComponent
                );

                subSnippet.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                    .AppendLine(componentRenderMethodCall)
                    .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
            }
            else
            {
                //Node is regular xml-node
                switch (tag)
                {
                    case "#text":
                        subSnippet.AppendLine(childNode.InnerText);
                        break;
                    case "#comment":
                        subSnippet.AppendLine($"<!-- {childNode.InnerText} -->");
                        break;
                    case "slot":
                        var slotName = GetNameFromNodeAttributes(attributeList);
                        subSnippet.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                            .AppendLine(CreateMethodCall($"__slotRenderer_{slotName.ToLower()}"))
                            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
                        break;

                    default:
                    {
                        var hasChildren = childNode.HasChildNodes;
                        subSnippet.AppendLine(CreateXmlOpeningTag(tag, attributeList, hasChildren));

                        if (hasChildren)
                        {
                            subSnippet.AppendLine(1,
                                ProcessNode(childNode, availableMtComponents, currentContext, rootComponent,
                                    parentComponent: parentComponent));
                            subSnippet.AppendLine(CreateXmlClosingTag(tag));
                        }

                        break;
                    }
                }
            }

            if (ifCondition != null)
            {
                subSnippet = WrapInIfStatement(subSnippet, ifCondition);
            }

            if (forEachCondition != null)
            {
                subSnippet = WrapInForeachLoop(subSnippet, forEachCondition);
                _loopDepth--;
            }

            snippet.AppendSnippet(subSnippet);
            nodeId++;
        }

        return snippet.ToString();
    }

    /// <summary>
    /// Recursively goes through all nodes in the components template and extracts the XML for each slot.
    /// All XML that is not inside a named slot, is appended to the "default" slot.
    /// </summary>
    /// <returns>
    /// A map of slot names and their contents.
    /// </returns>
    private Dictionary<string, string> GetSlotContentsGroupedBySlotName(XmlNode componentNode,
        MtComponent component, MtComponentMap availableMtComponents, MtDataContext context, MtComponent parentComponent,
        MtComponent rootComponent)
    {
        var contentsByName = new Dictionary<string, XmlNode>();

        foreach (XmlNode childNode in componentNode.ChildNodes)
        {
            if (childNode.Name != "template")
            {
                continue;
            }

            //Get the name from the slot attribute, or default if not found.
            var slotName = childNode.Attributes?["slot"]?.Value.ToLower() ?? "default";

            if (slotName.Trim().Length == 0)
            {
                throw new EmptyNodeAttributeException(
                    $"There's a template tag with empty slot name in <{componentNode.Name}></>.");
            }

            if (slotName == "default")
            {
                //Do not strip contents for default slot from node
                continue;
            }

            if (component.Slots.Contains(slotName))
            {
                //Only add templates, if the component does have a fitting slot
                contentsByName[slotName] = childNode.Clone();
            }

            componentNode.RemoveChild(childNode);
        }

        contentsByName["default"] = componentNode;

        return contentsByName.ToDictionary(
            kvp => kvp.Key,
            kvp => ProcessNode(kvp.Value, availableMtComponents, context, rootComponent, parentComponent)
        );
    }

    /// <summary>
    /// Process a node that has been identified as an component.
    /// </summary>
    private string ProcessComponentNode(
        int scope,
        MtComponent component,
        MtComponent parentComponent,
        MtDataContext currentContext,
        MtComponentAttributes attributeList,
        string componentBody,
        IReadOnlyDictionary<string, string> slotContents,
        MtComponent rootComponent
    )
    {
        foreach (var slotName in component.Slots)
        {
            var slotContent = "";
            if (slotContents.ContainsKey(slotName))
            {
                slotContent = slotContents[slotName];
            }

            _slots.Add(new MtComponentSlot
            {
                Scope = scope,
                Name = slotName,
                RenderMethodT4 = CreateSlotRenderMethod(
                    component,
                    scope,
                    currentContext,
                    slotName,
                    rootComponent,
                    slotContent,
                    parentComponent
                )
            });
        }

        var renderMethodName = GetComponentRenderMethodName(component, currentContext);
        if (!_renderMethods.ContainsKey(renderMethodName))
        {
            _renderMethods.Add(
                renderMethodName,
                CreateComponentRenderMethod(component, renderMethodName, componentBody, currentContext)
            );
        }

        //Create render call
        var renderComponentCall = new StringBuilder(renderMethodName + "(");

        //Create available arguments
        var renderArguments = new List<string>();

        //Add local variables to component render method call
        foreach (var localVariableName in currentContext.Keys)
        {
            renderArguments.Add($"{localVariableName}: {localVariableName}");
        }

        //Attach attributes to render method call
        foreach (var (attributeName, attributeValue) in attributeList)
        {
            if (component.Properties.TryGetValue(attributeName, out var value))
            {
                if (IsStringType(value))
                {
                    renderArguments.Add(
                        $"{attributeName}: {WrapIfString(value, ReplaceCurlyBraces(attributeValue, s => $@"{{({s})}}"))}");
                }
                else
                {
                    renderArguments.Add(
                        $"{attributeName}: {ReplaceCurlyBraces(attributeValue, s => $"({s})")}");
                }
            }
        }

        renderComponentCall.Append(string.Join(", ", renderArguments));

        //Append slot render calls
        if (component.Slots.Count > 0)
        {
            var i = 0;
            foreach (var slotName in component.Slots)
            {
                if (renderArguments.Count > 0 || i > 0)
                {
                    renderComponentCall.Append(", ");
                }

                renderComponentCall.Append($"__slotRenderer_{slotName}: ")
                    .Append("() => ")
                    .Append(GetSlotRenderMethodName(scope, slotName))
                    .Append('(');

                var slotArguments = new HashSet<string>();

                //Add local variables to slot render call
                foreach (var localVariableName in currentContext.Keys)
                {
                    slotArguments.Add($"{localVariableName}: {localVariableName}");
                }

                //Pass slot renderers
                if (parentComponent != rootComponent)
                {
                    foreach (var propertyName in parentComponent.Properties.Keys)
                    {
                        slotArguments.Add($"{propertyName}: {propertyName}");
                    }
                }

                foreach (var parentSlotName in parentComponent.Slots)
                {
                    slotArguments.Add($"__slotRenderer_{parentSlotName}: __slotRenderer_{parentSlotName}");
                }

                renderComponentCall.Append(string.Join(", ", slotArguments)).Append(')');

                i++;
            }
        }

        renderComponentCall.AppendLine(");");

        _namespaces.AddRange(component.Namespaces);

        return renderComponentCall.ToString();
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentRenderMethod(MtComponent component, string renderMethodName, string componentBody,
        MtDataContext currentContext)
    {
        var renderMethod = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .Append("private void ")
            .Append(renderMethodName)
            .Append('(');

        //open method arguments
        var arguments = new List<string>();

        //Add local variables to component render method call
        foreach (var (localVariableName, localVariableType) in currentContext)
        {
            arguments.Add($"{localVariableType} {localVariableName}");
        }

        //add slot render methods
        AppendSlotRenderArgumentsToList(arguments, component);

        //add component properties as arguments with defaults
        AppendComponentPropertiesToMethodArgumentsList(component, arguments);

        //close method arguments
        renderMethod.Append(string.Join(", ", arguments))
            .AppendLine(") {")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(componentBody);

        //insert mania scripts
        if (component.Scripts.Count > 0)
        {
            var scriptRenderMethodName = GetComponentScriptsRenderMethodName(component);
            var scriptRenderMethod = CreateComponentScriptsRenderMethod(component, scriptRenderMethodName);
            _maniaScriptRenderMethods.TryAdd(scriptRenderMethodName, scriptRenderMethod);

            var scriptArguments = new List<string>();

            //add method arguments with defaults
            scriptArguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null)
                .Select(property => property.Name));

            renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                .AppendLine(
                    $"__maniaScriptRenderMethods.Add(() => {scriptRenderMethodName}({string.Join(',', scriptArguments)}));")
                .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
        }

        return renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .Append('}')
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Takes all available properties of a component and adds them to the given list of method arguments.
    /// </summary>
    private static void AppendComponentPropertiesToMethodArgumentsList(MtComponent component, List<string> arguments)
    {
        arguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null)
            .Select(property => property.Default == null
                ? $"{property.Type} {property.Name}"
                : $"{property.Type} {property.Name} = {(WrapIfString(property, property.Default))}"));
    }

    /// <summary>
    /// Takes all available slots of a component and app ends the slot render arguments to the given list.
    /// </summary>
    private static void AppendSlotRenderArgumentsToList(List<string> arguments, MtComponent component)
    {
        arguments.AddRange(component.Slots.Select(slotName => $"Action __slotRenderer_{slotName}"));
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentScriptsRenderMethod(MtComponent component, string renderMethodName)
    {
        var renderMethod = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .Append("void ")
            .Append(renderMethodName)
            .Append('(');

        //open method arguments
        var arguments = new List<string>();

        //add method arguments with defaults
        arguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null).Select(property =>
            property.Default == null
                ? $"{property.Type} {property.Name}"
                : $"{property.Type} {property.Name} = {(WrapIfString(property, property.Default))}"));

        //close method arguments
        renderMethod.Append(string.Join(", ", arguments))
            .AppendLine(") {")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());

        //insert body
        renderMethod.AppendLine(_scriptTransformer.CreateManiaScriptBlock(component));

        return renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .Append('}')
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Checks the attribute list for if-condition and if found removes & returns it, else null.
    /// </summary>
    private string? GetIfConditionFromNodeAttributes(MtComponentAttributes attributeList)
    {
        return attributeList.ContainsKey("if") ? attributeList.Pull("if") : null;
    }

    /// <summary>
    /// Checks the attribute list for name and if found removes & returns it, else "default".
    /// </summary>
    private string GetNameFromNodeAttributes(MtComponentAttributes attributeList)
    {
        return attributeList.ContainsKey("name") ? attributeList.Pull("name") : "default";
    }

    /// <summary>
    /// Checks the attribute list for loop-condition and returns it, else null.
    /// </summary>
    private MtForeach? GetForeachConditionFromNodeAttributes(MtComponentAttributes attributeList, MtDataContext context,
        int nodeId)
    {
        return attributeList.ContainsKey("foreach")
            ? MtForeach.FromString(attributeList.Pull("foreach"), context, nodeId)
            : null;
    }

    /// <summary>
    /// Wraps the snippet in a if-condition.
    /// </summary>
    private Snippet WrapInIfStatement(Snippet input, string ifCondition)
    {
        var snippet = new Snippet();
        var ifContent = TemplateInterpolationRegex.Replace(ifCondition, "$1");

        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"if ({ifContent}) {{");
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, "}");
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Wraps the snippet in a foreach-loop.
    /// </summary>
    private Snippet WrapInForeachLoop(Snippet input, MtForeach foreachLoop)
    {
        var outerIndexVariableName = "__outerIndex" + (new Random()).Next();
        var innerIndexVariableName = "__index";

        if (_loopDepth > 1)
        {
            innerIndexVariableName += _loopDepth;
        }

        var snippet = new Snippet();
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"var {outerIndexVariableName} = 0;");
        snippet.AppendLine(null, $"foreach ({foreachLoop.Condition}) {{");
        snippet.AppendLine(null, $"var {innerIndexVariableName} = {outerIndexVariableName};");
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"{outerIndexVariableName}++;");
        snippet.AppendLine(null, "}");
        snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Parses the attributes of a XmlNode to an MtComponentAttributes-instance.
    /// </summary>
    public static MtComponentAttributes GetXmlNodeAttributes(XmlNode node)
    {
        var attributeList = new MtComponentAttributes();
        if (node.Attributes == null) return attributeList;

        foreach (XmlAttribute attribute in node.Attributes)
        {
            attributeList.Add(attribute.Name, attribute.Value);
        }

        return attributeList;
    }

    /// <summary>
    /// Returns C# code representation of the type.
    /// </summary>
    /// <param name="type">The type.</param>
    public static string GetFormattedName(Type type)
    {
        if (type.IsSubclassOf(typeof(DynamicObject)))
        {
            return "dynamic";
        }

        using var codeProvider = new CSharpCodeProvider();
        var typeReference = new CodeTypeReference(type);
        return codeProvider.GetTypeOutput(typeReference);
    }

    /// <summary>
    /// Creates a method call in the target language.
    /// </summary>
    private static string CreateMethodCall(string methodName, string methodArguments = "",
        string append = ";")
    {
        return $"{methodName}({methodArguments}){append}";
    }

    /// <summary>
    /// Returns the method name that renders the given component.
    /// </summary>
    private string GetComponentRenderMethodName(MtComponent component, MtDataContext context)
    {
        return $"Render_Component_{component.Id()}{context}";
    }

    /// <summary>
    /// Returns the method name that renders the scripts of a given component.
    /// </summary>
    private string GetComponentScriptsRenderMethodName(MtComponent component)
    {
        return $"Render_ComponentScript_{component.Id()}";
    }

    /// <summary>
    /// Returns the name of the method that renders the slot contents.
    /// </summary>
    private static string GetSlotRenderMethodName(int scope, string name)
    {
        return $"Render_Slot_{scope.GetHashCode()}_{name}";
    }

    /// <summary>
    /// Wrap the second argument in quotes, if the given property is a string type.
    /// </summary>
    private static string WrapIfString(MtComponentProperty property, string value)
    {
        return IsStringType(property) ? WrapStringInQuotes(value) : value;
    }

    /// <summary>
    /// Determines whether a component property is a string type.
    /// </summary>
    private static bool IsStringType(MtComponentProperty property)
    {
        return property.Type.ToLower().Contains("string"); //TODO: find better way to determine string
    }

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    public static string WrapStringInQuotes(string str)
    {
        return $@"$""{str}""";
    }

    /// <summary>
    /// Creates ManiaLink opening tag with version, name and id.
    /// 
    /// id = Identifies the ManiaLink for overwrite/delete.
    /// name = Shown in in-game debugger.
    /// version = Version for the markup language of Trackmania.
    /// </summary>
    public static string ManiaLinkStart(string name, int version = 3, string? displayLayer = null)
    {
        var layer = "";
        if (displayLayer != null)
        {
            layer += $@" layer=""{displayLayer}""";
        }

        return $@"<manialink version=""{version}"" id=""{name}"" name=""EvoSC#-{name}""{layer}>";
    }

    /// <summary>
    /// Creates ManiaLink closing tag.
    /// </summary>
    private static string ManiaLinkEnd()
    {
        return "</manialink>";
    }

    /// <summary>
    /// Creates a xml opening tag for the given string and attribute list.
    /// </summary>
    public string CreateXmlOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren)
    {
        var output = $"<{tag}";

        foreach (var (attributeName, attributeValue) in attributeList)
        {
            output +=
                @$" {attributeName}=""{ReplaceCurlyBraces(attributeValue, _maniaTemplateLanguage.InsertResult)}""";
        }

        if (!hasChildren)
        {
            output += " /";
        }

        return output + ">";
    }

    /// <summary>
    /// Creates a xml closing tag for the given string.
    /// </summary>
    private static string CreateXmlClosingTag(string tag)
    {
        return $"</{tag}>";
    }

    /// <summary>
    /// Converts any valid XML-string into an XmlNode-element.
    /// </summary>
    public static XmlNode XmlStringToNode(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }

    /// <summary>
    /// Joins consecutive feature blocks to reduce generated code.
    /// </summary>
    public static string JoinFeatureBlocks(string manialink)
    {
        var match = TemplateFeatureControlRegex.Match(manialink);
        var output = new Snippet();

        while (match.Success)
        {
            manialink = manialink.Replace(match.ToString(), "\n");
            match = match.NextMatch();
        }

        foreach (var line in manialink.Split('\n'))
        {
            if (line.Trim().Length > 0)
            {
                output.AppendLine(line);
            }
        }

        return output.ToString();
    }
}