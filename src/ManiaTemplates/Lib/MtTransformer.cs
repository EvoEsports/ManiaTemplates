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
    private int _conditionChainCount;

    /// <summary>
    /// Tag of nodes that only render their children, without producing an element themselves.
    /// </summary>
    private const string FragmentTag = "template";

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
            maniaTemplateLanguage.Context("import namespace=\"System.Collections.Generic\""),
            maniaTemplateLanguage.Context("import namespace=\"ManiaTemplates.Lib\""),
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
            if (propertyValue is null)
            {
                continue;
            }

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
            if (propertyValue is null)
            {
                continue;
            }

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
        string? openConditionChainVariable = null;
        foreach (XmlNode childNode in node.ChildNodes)
        {
            var subSnippet = new Snippet();
            var tag = childNode.Name;
            var attributeList = IXmlMethods.GetAttributes(childNode);
            var currentContext = oldContext;

            var loop = attributeList.PullLoop(oldContext, nodeId, _loopDepth + 1);
            var condition = attributeList.PullCondition();

            if (loop != null && condition != null)
            {
                throw new InvalidConditionException(
                    $"The {condition.AttributeName} attribute on <{tag}> must not be combined with a {loop.Keyword} loop, put them on separate nodes instead.");
            }

            var conditionChainVariable = GetConditionChainVariable(childNode, tag, condition, openConditionChainVariable);

            if (loop != null)
            {
                currentContext = loop.Context;
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
                    case FragmentTag:
                        //Fragments only render their children, which allows grouping nodes for conditions and loops
                        EnsureFragmentHasNoAttributes(attributeList);
                        subSnippet.AppendLine(ProcessNode(childNode, componentMap, currentContext, rootComponent,
                            parentComponent: parentComponent));
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
                                    attributeList[originalAttributeName] = new MtComponentAttribute
                                    {
                                        Value = maniaTemplateLanguage.InsertResultEscaped(aliasAttributeName)
                                    };
                                }
                                else
                                {
                                    //Resolve alias on node
                                    attributeList.Add(originalAttributeName, new MtComponentAttribute
                                    {
                                        Value = maniaTemplateLanguage.InsertResultEscaped(aliasAttributeName),
                                        Alias = aliasAttributeName,
                                        IsFallthroughAttribute = true
                                    });
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

            if (condition != null)
            {
                subSnippet = CreateConditionBlock(subSnippet, condition, conditionChainVariable);
            }

            if (loop != null)
            {
                subSnippet = CreateLoopBlock(subSnippet, loop);
                _loopDepth--;
            }

            //Comments may be placed between the nodes of a condition chain
            if (childNode is not XmlComment)
            {
                openConditionChainVariable = condition?.Type == MtConditionType.Else ? null : conditionChainVariable;
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

        //Iterate over a copy, as named slot templates are removed from the node
        foreach (var childNode in componentNode.ChildNodes.Cast<XmlNode>().ToList())
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
        foreach (var (attributeName, attribute) in attributeList)
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
                if (templateContentNode.ChildNodes.Count != 1 || templateContentNode.FirstChild?.Name == FragmentTag)
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
                    ICurlyBraceMethods.ReplaceCurlyBraces(attribute.Value, s => $"{{({s})}}"))
                : ICurlyBraceMethods.ReplaceCurlyBraces(attribute.Value, s => $"({s})");

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

        //add slot render methods
        AppendSlotRenderArgumentsToList(component, arguments);

        //add component properties as arguments with defaults
        AppendComponentPropertiesArgumentsList(component, arguments);

        //Add fallthrough variables to component render method call
        arguments.AddRange(aliasMap.Values.Select(aliasAttributeName => $"string? {aliasAttributeName} = null"));

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

        return CreateRenderMethod(methodName, methodArguments, slotContent);
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
    /// Throws if a fragment has attributes left after pulling conditions and loops, as they can not be rendered.
    /// </summary>
    private static void EnsureFragmentHasNoAttributes(MtComponentAttributes attributeList)
    {
        //Default slot contents may be wrapped in <template slot="default">
        if (attributeList.TryGetValue("slot", out var slotAttribute) &&
            slotAttribute.Value.Trim().Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            attributeList.Remove("slot");
        }

        if (attributeList.Count == 0)
        {
            return;
        }

        throw new UnsupportedAttributeException(
            $"<{FragmentTag}> only supports conditions and loops, found: {string.Join(", ", attributeList.Keys)}. Named slots can only be filled in direct children of components.");
    }

    /// <summary>
    /// Returns the name of the variable tracking whether a condition of the chain the node belongs to was met.
    /// Returns null for nodes without condition and if-conditions not continued by an else-if or else.
    /// </summary>
    private string? GetConditionChainVariable(XmlNode node, string tag, MtCondition? condition,
        string? openConditionChainVariable)
    {
        switch (condition?.Type)
        {
            case null:
                return null;

            case MtConditionType.If:
                return IsConditionChainContinued(node) ? $"__conditionChain{++_conditionChainCount}" : null;

            default:
                return openConditionChainVariable ?? throw new InvalidConditionException(
                    $"The {condition.AttributeName} attribute on <{tag}> must follow a node with an if or else-if attribute, only comments may be in between.");
        }
    }

    /// <summary>
    /// Checks whether the next sibling node, ignoring comments, continues a condition chain with else-if or else.
    /// </summary>
    private static bool IsConditionChainContinued(XmlNode node)
    {
        var sibling = node.NextSibling;
        while (sibling is XmlComment)
        {
            sibling = sibling.NextSibling;
        }

        return sibling?.Attributes?["else-if"] != null || sibling?.Attributes?["else"] != null;
    }

    /// <summary>
    /// Wraps the snippet in a condition block.
    /// Conditions of a chain are rendered as separate if-blocks sharing a variable that tracks whether a condition
    /// was met, which allows rendering comments between them.
    /// </summary>
    private Snippet CreateConditionBlock(Snippet input, MtCondition condition, string? conditionChainVariable)
    {
        var snippet = new Snippet();
        var expression = ICurlyBraceMethods.TemplateInterpolationRegex.Replace(condition.Expression, "$1");

        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());

        if (conditionChainVariable == null)
        {
            snippet.AppendLine(null, $"if ({expression}) {{");
        }
        else
        {
            switch (condition.Type)
            {
                case MtConditionType.If:
                    snippet.AppendLine(null, $"var {conditionChainVariable} = false;");
                    snippet.AppendLine(null, $"if ({expression}) {{");
                    break;
                case MtConditionType.ElseIf:
                    snippet.AppendLine(null, $"if (!{conditionChainVariable} && ({expression})) {{");
                    break;
                default:
                    snippet.AppendLine(null, $"if (!{conditionChainVariable}) {{");
                    break;
            }

            if (condition.Type != MtConditionType.Else)
            {
                snippet.AppendLine(null, $"{conditionChainVariable} = true;");
            }
        }

        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, "}");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Wraps the snippet in a loop, exposing the zero-based iteration index to the loop body.
    /// The surrounding block scopes the iteration counter, so sibling loops do not collide.
    /// </summary>
    private Snippet CreateLoopBlock(Snippet input, MtLoop loop)
    {
        var snippet = new Snippet();
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, "{");
        snippet.AppendLine(null, $"var {loop.CounterVariableName} = 0;");
        snippet.AppendLine(null, $"{loop.Keyword} ({loop.Condition}) {{");
        snippet.AppendLine(null, $"var {loop.IndexVariableName} = {loop.CounterVariableName}++;");
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(null, maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine(null, "}");
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
        return "FT_" + Regex.Replace(variableName.Replace("-", "HYPH"), @"\W", "");
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