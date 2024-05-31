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

public class MtTransformer(ManiaTemplateEngine engine, IManiaTemplateLanguage maniaTemplateLanguage)
    : IXmlMethods, IStringMethods
{
    private readonly MtScriptTransformer _scriptTransformer = new(maniaTemplateLanguage);
    private readonly List<string> _namespaces = [];
    private readonly List<MtComponentSlot> _slots = [];
    private readonly Dictionary<string, string> _renderMethods = new();
    private readonly Dictionary<string, string> _maniaScriptRenderMethods = new();
    private int _loopDepth;

    /// <summary>
    /// Creates the target language template, which can be pre-processed for faster rendering.
    /// </summary>
    public string BuildManialink(MtComponent rootComponent, string className, int version = 3)
    {
        _namespaces.AddRange(rootComponent.Namespaces);

        var renderBodyArguments = string.Join(',', Enumerable.Repeat("DoNothing", rootComponent.Slots.Count));

        var body = ProcessNode(
            IXmlMethods.NodeFromString(rootComponent.TemplateContent),
            engine.BaseMtComponents.Overload(rootComponent.ImportedComponents),
            new MtDataContext(),
            rootComponent,
            rootComponent
        );

        var template = new Snippet
        {
            maniaTemplateLanguage.Context(@"template language=""C#"""), //Might not be needed
            maniaTemplateLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            maniaTemplateLanguage.Context(@"import namespace=""System.Security"""),
            CreateImportStatements(),
            ManiaLink.OpenTag(className, version, rootComponent.DisplayLayer),
            "<#",
            $"RenderBody({renderBodyArguments});",
            "#>",
            ManiaLink.CloseTag(),
            CreateTemplatePropertiesBlock(rootComponent),
            CreateInsertedManiaScriptsList(),
            CreateDoNothingMethod(),
            CreateBodyRenderMethod(body, rootComponent),
            CreateRenderMethodsBlock()
        };

        return maniaTemplateLanguage.OptimizeOutput(template.ToString());
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtComponent rootComponent)
    {
        var methodArguments = new List<string>();
        AppendSlotRenderArgumentsToList(rootComponent, methodArguments);
        var bodyRenderMethod = new StringBuilder($"private void RenderBody({string.Join(',', methodArguments)}) {{\n");

        //Root mania script block
        var rootScriptBlock = "";
        if (rootComponent.Scripts.Count > 0)
        {
            rootScriptBlock = _scriptTransformer.CreateManiaScriptBlock(rootComponent);
        }

        var subManiaScripts = new StringBuilder(maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine(
                "foreach(var maniaScriptRenderMethod in __maniaScriptRenderMethods){ maniaScriptRenderMethod(); }")
            .AppendLine(maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();

        //Render content
        bodyRenderMethod.AppendLine(maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(_scriptTransformer.CreateManiaScriptDirectivesBlock())
            .AppendLine(body)
            .AppendLine(subManiaScripts)
            .AppendLine(rootScriptBlock)
            .AppendLine(maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}");

        return maniaTemplateLanguage.FeatureBlock(bodyRenderMethod.ToString()).ToString();
    }

    /// <summary>
    /// Takes all loaded namespaces and creates the import statements for the target language.
    /// </summary>
    private string CreateImportStatements()
    {
        var imports = new StringBuilder();

        foreach (var propertyValue in engine.GlobalVariables.Values)
        {
            var nameSpace = propertyValue.GetType().Namespace;

            if (nameSpace != "System")
            {
                imports.AppendLine(maniaTemplateLanguage.Context($@"import namespace=""{nameSpace}"""));
            }
        }

        foreach (var nameSpace in _namespaces)
        {
            imports.AppendLine(maniaTemplateLanguage.Context($@"import namespace=""{nameSpace}"""));
        }

        return imports.ToString();
    }

    /// <summary>
    /// Creates a block containing the templates properties, which are filled when rendered.
    /// </summary>
    private string CreateTemplatePropertiesBlock(MtComponent mtComponent)
    {
        var properties = new StringBuilder();

        foreach (var (propertyName, propertyValue) in engine.GlobalVariables)
        {
            var type = propertyValue.GetType();

            properties.AppendLine(maniaTemplateLanguage
                .FeatureBlock($"public {GetFormattedTypeName(type)} ?{propertyName} {{ get; init; }}").ToString());
        }

        foreach (var property in mtComponent.Properties.Values)
        {
            properties.AppendLine(maniaTemplateLanguage
                .FeatureBlock(
                    $"public {property.Type} {property.Name} {{ get; init; }}{(property.Default == null ? "" : $" = {property.GetDefaultWrapped()};")}")
                .ToString());
        }

        return properties.ToString();
    }

    /// <summary>
    /// Process a ManiaTemplate node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap componentMap, MtDataContext oldContext,
        MtComponent rootComponent, MtComponent parentComponent,
        Dictionary<string, string>? fallthroughAttributesMap = null)
    {
        Snippet snippet = [];

        var nodeId = 1;
        foreach (XmlNode childNode in node.ChildNodes)
        {
            var subSnippet = new Snippet();
            var tag = childNode.Name;
            var attributeList = IXmlMethods.GetAttributes(childNode);
            var currentContext = oldContext;

            var forEachCondition = attributeList.PullForeachCondition(oldContext, nodeId, _loopDepth);
            var ifCondition = attributeList.PullIfCondition();

            if (forEachCondition != null)
            {
                currentContext = forEachCondition.Context;
                _loopDepth++;
            }

            if (componentMap.TryGetValue(tag, out var importedComponent))
            {
                //Node is a component
                var component = engine.GetComponent(importedComponent.TemplateKey);
                var slotContents = GetSlotContentsGroupedBySlotName(childNode, component, componentMap, currentContext,
                    parentComponent, rootComponent);

                var oldLoopDepth = _loopDepth;
                _loopDepth = 0;
                var componentRenderMethodCall = ProcessComponentNode(
                    childNode.GetHashCode(),
                    component,
                    parentComponent,
                    currentContext,
                    oldContext,
                    attributeList,
                    slotContents,
                    rootComponent,
                    componentMap
                );
                _loopDepth = oldLoopDepth;

                subSnippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart())
                    .AppendLine(componentRenderMethodCall)
                    .AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
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
                        var slotName = attributeList.PullName().ToLower();
                        subSnippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart())
                            .AppendLine($"{GetSlotRendererVariableName(slotName)}();")
                            .AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
                        break;

                    default:
                    {
                        var hasChildren = childNode.HasChildNodes;

                        if (fallthroughAttributesMap != null && node.ChildNodes.Count == 1)
                        {
                            foreach (var (originalAttributeName, aliasAttributeName) in fallthroughAttributesMap)
                            {
                                if (attributeList.ContainsKey(originalAttributeName))
                                {
                                    //Overwrite existing attribute with fallthrough value
                                    attributeList[originalAttributeName] =
                                        maniaTemplateLanguage.InsertResult(aliasAttributeName);
                                }
                                else
                                {
                                    //Resolve alias on node
                                    attributeList.Add(originalAttributeName,
                                        maniaTemplateLanguage.InsertResult(aliasAttributeName));
                                }
                            }
                        }

                        subSnippet.AppendLine(IXmlMethods.CreateOpeningTag(tag, attributeList, hasChildren,
                            curlyContentWrapper: maniaTemplateLanguage.InsertResultEscaped));

                        if (hasChildren)
                        {
                            subSnippet.AppendLine(1,
                                ProcessNode(childNode, componentMap, currentContext, rootComponent,
                                    parentComponent: parentComponent));
                            subSnippet.AppendLine(IXmlMethods.CreateClosingTag(tag));
                        }

                        break;
                    }
                }
            }

            if (ifCondition != null)
            {
                subSnippet = CreateIfBlock(subSnippet, ifCondition);
            }

            if (forEachCondition != null)
            {
                subSnippet = CreateForEachBlock(subSnippet, forEachCondition);
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
        MtDataContext oldContext,
        MtComponentAttributes attributeList,
        Dictionary<string, string> slotContents,
        MtComponent rootComponent,
        MtComponentMap componentMap
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
                    scope,
                    currentContext,
                    slotName,
                    slotContent,
                    rootComponent,
                    parentComponent
                )
            });
        }

        var templateContentNode = IXmlMethods.NodeFromString(component.TemplateContent);
        var renderMethodName = GetComponentRenderMethodName(component, currentContext);
        var fallthroughAttributesAliasMap = new Dictionary<string, string>();

        //Create render call
        var renderComponentCall = new StringBuilder(renderMethodName + "(");

        //Create available arguments
        var componentRenderArguments = new List<string>();

        //Attach attributes to render method call
        foreach (var (attributeName, attributeValue) in attributeList)
        {
            bool isStringType;
            string attributeNameAlias;

            //Skip attributes that don't match component property name
            if (component.Properties.TryGetValue(attributeName, out var componentProperty))
            {
                isStringType = componentProperty.IsStringType();
                attributeNameAlias = attributeName;
            }
            else
            {
                if (templateContentNode.ChildNodes.Count != 1)
                {
                    //Only add fallthrough attributes if the component template has only one root element
                    continue;
                }
                
                isStringType = true;
                attributeNameAlias = GetFallthroughAttributeAlias(attributeName);
                fallthroughAttributesAliasMap[attributeName] = attributeNameAlias;
            }

            var methodArgument = isStringType
                ? IStringMethods.WrapStringInQuotes(
                    ICurlyBraceMethods.ReplaceCurlyBraces(attributeValue, s => $"{{({s})}}"))
                : ICurlyBraceMethods.ReplaceCurlyBraces(attributeValue, s => $"({s})");

            componentRenderArguments.Add(CreateMethodCallArgument(attributeNameAlias, methodArgument));
        }

        renderComponentCall.Append(string.Join(", ", componentRenderArguments));

        //Append slot render calls
        if (component.Slots.Count > 0)
        {
            var i = 0;
            foreach (var slotName in component.Slots)
            {
                if (componentRenderArguments.Count > 0 || i > 0)
                {
                    renderComponentCall.Append(", ");
                }

                renderComponentCall.Append($"{GetSlotRendererVariableName(slotName)}: () => ")
                    .Append(GetSlotRenderMethodName(scope, slotName))
                    .Append('(');

                var slotArguments = new HashSet<string>();

                //Add local variables to slot render call (passes loop index and fallthrough vars)
                foreach (var localVariableName in currentContext.Keys)
                {
                    slotArguments.Add(CreateMethodCallArgument(localVariableName));
                }

                //Add parent properties as arguments
                if (parentComponent != rootComponent)
                {
                    foreach (var propertyName in parentComponent.Properties.Keys)
                    {
                        slotArguments.Add(CreateMethodCallArgument(propertyName));
                    }
                }

                //Pass slot renderers
                foreach (var slotRenderVariable in parentComponent.Slots.Select(GetSlotRendererVariableName))
                {
                    slotArguments.Add(CreateMethodCallArgument(slotRenderVariable));
                }

                renderComponentCall.Append(string.Join(", ", slotArguments))
                    .Append(')');

                i++;
            }
        }

        renderComponentCall.AppendLine(");");

        _namespaces.AddRange(component.Namespaces);

        if (!_renderMethods.ContainsKey(renderMethodName))
        {
            var componentBody = ProcessNode(
                templateContentNode,
                componentMap.Overload(component.ImportedComponents),
                oldContext,
                rootComponent: rootComponent,
                parentComponent: component,
                fallthroughAttributesMap: fallthroughAttributesAliasMap
            );

            _renderMethods.Add(
                renderMethodName,
                CreateComponentRenderMethod(component, renderMethodName, componentBody, fallthroughAttributesAliasMap)
            );
        }

        return renderComponentCall.ToString();
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentRenderMethod(MtComponent component, string renderMethodName, string componentBody,
        Dictionary<string, string> aliasMap)
    {
        //open method arguments
        var arguments = new List<string>();
        var body = new StringBuilder(componentBody);

        //Add fallthrough variables to component render method call
        arguments.AddRange(aliasMap.Values.Select(aliasAttributeName => $"string {aliasAttributeName}"));

        //add slot render methods
        AppendSlotRenderArgumentsToList(component, arguments);

        //add component properties as arguments with defaults
        AppendComponentPropertiesArgumentsList(component, arguments);

        //Insert mania scripts
        if (component.Scripts.Count > 0)
        {
            var scriptRenderMethodName = GetComponentScriptsRenderMethodName(component);
            var scriptRenderMethod = CreateComponentScriptsRenderMethod(component, scriptRenderMethodName);
            _maniaScriptRenderMethods.TryAdd(scriptRenderMethodName, scriptRenderMethod);

            var scriptArguments = new List<string>();

            //add method arguments with defaults
            scriptArguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null)
                .Select(property => property.Name));

            body.AppendLine(maniaTemplateLanguage.FeatureBlockStart())
                .AppendLine(
                    $"__maniaScriptRenderMethods.Add(() => {scriptRenderMethodName}({string.Join(',', scriptArguments)}));")
                .AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
        }

        return CreateRenderMethod(renderMethodName, arguments, body.ToString());
    }

    /// <summary>
    /// Creates the slot-render method for a given data context.
    /// </summary>
    private string CreateSlotRenderMethod(int scope, MtDataContext context, string slotName, string slotContent,
        MtComponent rootComponent, MtComponent parentComponent)
    {
        var methodArguments = new List<string>();
        var methodName = GetSlotRenderMethodName(scope, slotName);
        var localVariables = new List<string>();

        //Add slot render methods.
        AppendSlotRenderArgumentsToList(parentComponent, methodArguments);

        //Add component properties as arguments.
        foreach (var (localVariableName, localVariableType) in context)
        {
            //Don't add properties present in root component, because they're available everywhere.
            if (!rootComponent.Properties.ContainsKey(localVariableName))
            {
                methodArguments.Add($"{localVariableType} {localVariableName}");
            }
        }

        if (parentComponent != rootComponent)
        {
            AppendComponentPropertiesArgumentsList(parentComponent, methodArguments);
        }

        return CreateRenderMethod(methodName, methodArguments, slotContent, localVariables);
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentScriptsRenderMethod(MtComponent component, string renderMethodName)
    {
        var arguments = new List<string>();

        //Add method arguments with defaults
        AppendComponentPropertiesArgumentsList(component, arguments);

        return CreateRenderMethod(renderMethodName, arguments, _scriptTransformer.CreateManiaScriptBlock(component));
    }

    /// <summary>
    /// Creates a method to render template contents.
    /// </summary>
    private string CreateRenderMethod(string name, IEnumerable<string> arguments, string body,
        IEnumerable<string>? localVariableDeclarations = null)
    {
        var renderMethod = new StringBuilder(maniaTemplateLanguage.FeatureBlockStart())
            .Append($"private void {name}(")
            .Append(string.Join(", ", arguments))
            .AppendLine(") {");

        if (localVariableDeclarations != null)
        {
            foreach (var declaration in localVariableDeclarations)
            {
                renderMethod.AppendLine(declaration);
            }
        }

        return renderMethod.AppendLine(maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(body)
            .AppendLine(maniaTemplateLanguage.FeatureBlockStart())
            .Append('}')
            .AppendLine(maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Takes all available properties of a component and adds them to the given list of method arguments.
    /// </summary>
    private static void AppendComponentPropertiesArgumentsList(MtComponent component, List<string> arguments)
    {
        arguments.AddRange(component.Properties.Values
            .OrderBy(property => property.Default != null)
            .Select(property => property.ToMethodArgument()));
    }

    /// <summary>
    /// Takes all available slots of a component and app ends the slot render arguments to the given list.
    /// </summary>
    private static void AppendSlotRenderArgumentsToList(MtComponent component, List<string> arguments)
    {
        arguments.AddRange(component.Slots.Select(slotName => $"Action {GetSlotRendererVariableName(slotName)}"));
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateDoNothingMethod()
    {
        return maniaTemplateLanguage.FeatureBlock("private static void DoNothing(){}").ToString();
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateInsertedManiaScriptsList()
    {
        return new StringBuilder()
            .AppendLine(maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("private List<string> __insertedOneTimeManiaScripts = new List<string>();")
            .AppendLine("private List<Action> __maniaScriptRenderMethods = new List<Action>();")
            .AppendLine(maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
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
    /// Wraps the snippet in a if-condition.
    /// </summary>
    private Snippet CreateIfBlock(Snippet input, string ifCondition)
    {
        var snippet = new Snippet();
        var ifContent = ICurlyBraceMethods.TemplateInterpolationRegex.Replace(ifCondition, "$1");

        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"if ({ifContent}) {{");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, "}");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Wraps the snippet in a foreach-loop.
    /// </summary>
    private Snippet CreateForEachBlock(Snippet input, MtForeach foreachLoop)
    {
        var outerIndexVariableName = "__outerIndex" + (new Random()).Next();
        var innerIndexVariableName = "__index";

        if (_loopDepth > 1)
        {
            innerIndexVariableName += _loopDepth;
        }

        var snippet = new Snippet();
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"var {outerIndexVariableName} = 0;");
        snippet.AppendLine(null, $"foreach ({foreachLoop.Condition}) {{");
        snippet.AppendLine(null, $"var {innerIndexVariableName} = {outerIndexVariableName};");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, $"{outerIndexVariableName}++;");
        snippet.AppendLine(null, "}");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Returns C# code representation of the type.
    /// </summary>
    /// <param name="type">The type.</param>
    public static string GetFormattedTypeName(Type type)
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
    /// Returns the method name that renders the given component.
    /// </summary>
    private static string GetComponentRenderMethodName(MtComponent component, MtDataContext context)
    {
        return $"Render_Component_{component.Id()}{context}";
    }

    /// <summary>
    /// Returns a valid variable name alias.
    /// </summary>
    private static string GetFallthroughAttributeAlias(string variableName)
    {
        return Regex.Replace(variableName, @"\W", "") + new Random().Next();
    }

    /// <summary>
    /// Returns the method name that renders the scripts of a given component.
    /// </summary>
    private static string GetComponentScriptsRenderMethodName(MtComponent component)
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
    /// Returns the name of the variable that renders a slot.
    /// </summary>
    private static string GetSlotRendererVariableName(string slotName)
    {
        return $"__slotRenderer_{slotName}";
    }

    /// <summary>
    /// Formats local variables to be passed to a render method.
    /// </summary>
    private static string CreateMethodCallArgument(string argumentName)
    {
        return CreateMethodCallArgument(argumentName, argumentName);
    }

    /// <summary>
    /// Formats local variables to be passed to a render method.
    /// It uses the argument name if no value is passed as second argument.
    /// </summary>
    private static string CreateMethodCallArgument(string argumentName, string argumentValue)
    {
        return $"{argumentName}: {argumentValue}";
    }
}