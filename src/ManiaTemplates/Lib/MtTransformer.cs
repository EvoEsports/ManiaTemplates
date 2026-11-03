using System.CodeDom;
using System.Dynamic;
using System.Text;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;
using Microsoft.CSharp;

namespace ManiaTemplates.Lib;

public class MtTransformer(ManiaTemplateEngine engine, IManiaTemplateLanguage maniaTemplateLanguage)
{
    private readonly MtScriptTransformer _scriptTransformer = new(maniaTemplateLanguage);
    private readonly MtSlotTransformer _slotTransformer = new(maniaTemplateLanguage);
    private readonly MtControlFlowTransformer _controlFlowTransformer = new(maniaTemplateLanguage);
    private readonly List<string> _namespaces = [];
    private readonly Dictionary<string, string> _renderMethods = new();
    private readonly Dictionary<string, string> _maniaScriptRenderMethods = new();
    private int _loopDepth;

    /// <summary>
    /// Tag of nodes that only render their children, without producing an element themselves.
    /// </summary>
    internal const string FragmentTag = "template";

    /// <summary>
    /// Creates the target language template, which can be pre-processed for faster rendering.
    /// </summary>
    public string BuildManialink(MtComponent rootComponent, string className, int version = 3)
    {
        _namespaces.AddRange(rootComponent.Namespaces);

        // Slots of the rendered component are not filled
        var renderBodyArguments = string.Join(", ", Enumerable.Repeat("null", rootComponent.Slots.Count));

        var body = ProcessNode(
            XmlUtils.NodeFromString(rootComponent.TemplateContent),
            engine.BaseMtComponents.Overload(rootComponent.ImportedComponents),
            new MtDataContext(),
            rootComponent
        );

        var template = new MtCodeSnippet
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
            CreateBodyRenderMethod(body, rootComponent),
            CreateRenderMethodsBlock(),
            _slotTransformer.CreateSlotRendererDelegates()
        };

        return maniaTemplateLanguage.OptimizeOutput(template.ToString());
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtComponent rootComponent)
    {
        var methodArguments = new List<string>();
        _slotTransformer.AppendSlotRendererParameters(rootComponent, methodArguments);
        var bodyRenderMethod = new StringBuilder($"private void RenderBody({string.Join(',', methodArguments)}) {{\n");

        // Root mania script block
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

        // Render content
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
    /// Processes the children of a node, applying the conditions and loops placed on them.
    /// Fallthrough attributes are applied to the child, if it is the only one.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap componentMap, MtDataContext context,
        MtComponent parentComponent, Dictionary<string, string>? fallthroughAttributes = null)
    {
        MtCodeSnippet snippet = [];
        var childFallthroughAttributes = node.ChildNodes.Count == 1 ? fallthroughAttributes : null;

        var nodeId = 1;
        string? openConditionChainVariable = null;
        foreach (XmlNode childNode in node.ChildNodes)
        {
            var attributeList = XmlUtils.GetAttributes(childNode);
            var loop = attributeList.PullLoop(context, nodeId, _loopDepth + 1);
            var condition = attributeList.PullCondition();

            MtControlFlowTransformer.EnsureLoopAndConditionAreNotCombined(childNode.Name, loop, condition);
            var conditionChainVariable =
                _controlFlowTransformer.GetConditionChainVariable(childNode, condition, openConditionChainVariable);

            if (loop != null)
            {
                _loopDepth++;
            }

            var childSnippet = new MtCodeSnippet().AppendLine(ProcessChildNode(childNode, attributeList, componentMap,
                loop?.Context ?? context, parentComponent, loop, condition, childFallthroughAttributes));

            if (condition != null)
            {
                childSnippet =
                    _controlFlowTransformer.CreateConditionBlock(childSnippet, condition, conditionChainVariable);
            }

            if (loop != null)
            {
                childSnippet = _controlFlowTransformer.CreateLoopBlock(childSnippet, loop);
                _loopDepth--;
            }

            // Comments may be placed between the nodes of a condition chain
            if (childNode is not XmlComment)
            {
                openConditionChainVariable = condition?.Type == MtConditionType.Else ? null : conditionChainVariable;
            }

            snippet.AppendSnippet(childSnippet);
            nodeId++;
        }

        return snippet.ToString();
    }

    /// <summary>
    /// Processes a single node, depending on whether it is a component, text, comment, slot, fragment or element.
    /// </summary>
    private string ProcessChildNode(XmlNode node, MtComponentAttributes attributeList, MtComponentMap componentMap,
        MtDataContext context, MtComponent parentComponent, MtLoop? loop, MtCondition? condition,
        Dictionary<string, string>? fallthroughAttributes)
    {
        if (componentMap.TryGetValue(node.Name, out var importedComponent))
        {
            var component = engine.GetComponent(importedComponent.TemplateKey);
            return new StringBuilder()
                .AppendLine(maniaTemplateLanguage.FeatureBlockStart())
                .AppendLine(ProcessComponentNode(node, component, parentComponent, context, attributeList,
                    componentMap))
                .Append(maniaTemplateLanguage.FeatureBlockEnd())
                .ToString();
        }

        return node.Name switch
        {
            "#text" => node.InnerText,
            "#comment" => $"<!-- {node.InnerText} -->",
            "slot" => _slotTransformer.CreateSlotCall(parentComponent, attributeList, context),
            FragmentTag => ProcessFragmentNode(node, attributeList, componentMap, context, parentComponent, loop,
                condition),
            _ => ProcessElementNode(node, attributeList, componentMap, context, parentComponent,
                fallthroughAttributes)
        };
    }

    /// <summary>
    /// Processes a fragment, which only renders its children and allows grouping nodes for conditions and loops.
    /// </summary>
    private string ProcessFragmentNode(XmlNode node, MtComponentAttributes attributeList,
        MtComponentMap componentMap, MtDataContext context, MtComponent parentComponent, MtLoop? loop,
        MtCondition? condition)
    {
        if (attributeList.ContainsKey("slot"))
        {
            MtSlotTransformer.EnsureSlotTemplateHasNoControlFlow(loop, condition);
        }

        EnsureFragmentHasNoAttributes(attributeList);

        return ProcessNode(node, componentMap, context, parentComponent);
    }

    /// <summary>
    /// Processes a regular XML element and its children.
    /// </summary>
    private string ProcessElementNode(XmlNode node, MtComponentAttributes attributeList,
        MtComponentMap componentMap, MtDataContext context, MtComponent parentComponent,
        Dictionary<string, string>? fallthroughAttributes)
    {
        if (fallthroughAttributes != null)
        {
            ApplyFallthroughAttributes(attributeList, fallthroughAttributes);
        }

        var element = new StringBuilder(XmlUtils.CreateOpeningTag(node.Name, attributeList, node.HasChildNodes,
            curlyContentWrapper: maniaTemplateLanguage.InsertResultEscaped));

        if (node.HasChildNodes)
        {
            element.AppendLine()
                .AppendLine(ProcessNode(node, componentMap, context, parentComponent))
                .Append(XmlUtils.CreateClosingTag(node.Name));
        }

        return element.ToString();
    }

    /// <summary>
    /// Sets the attributes passed to a component, that are not properties of it, on its root element.
    /// They are passed to the render method of the component as variables with an alias name.
    /// </summary>
    private void ApplyFallthroughAttributes(MtComponentAttributes attributeList,
        Dictionary<string, string> fallthroughAttributes)
    {
        foreach (var (attributeName, aliasName) in fallthroughAttributes)
        {
            if (attributeList.ContainsKey(attributeName))
            {
                // Overwrite existing attribute with fallthrough value
                attributeList[attributeName] = new MtComponentAttribute
                {
                    Value = maniaTemplateLanguage.InsertResultEscaped(aliasName)
                };
            }
            else
            {
                attributeList.Add(attributeName, new MtComponentAttribute
                {
                    Value = maniaTemplateLanguage.InsertResultEscaped(aliasName),
                    Alias = aliasName,
                    IsFallthroughAttribute = true
                });
            }
        }
    }

    /// <summary>
    /// Process a node that has been identified as an component.
    /// </summary>
    private string ProcessComponentNode(
        XmlNode componentNode,
        MtComponent component,
        MtComponent parentComponent,
        MtDataContext currentContext,
        MtComponentAttributes attributeList,
        MtComponentMap componentMap
    )
    {
        var scope = componentNode.GetHashCode();
        var templateContentNode = XmlUtils.NodeFromString(component.TemplateContent);
        var renderMethodName = MtCodeNames.ComponentRenderMethod(component, currentContext);
        var fallthroughAttributesAliasMap = new Dictionary<string, string>();
        var componentRenderArguments = CreateComponentArguments(component, attributeList, templateContentNode,
            fallthroughAttributesAliasMap);

        _namespaces.AddRange(component.Namespaces);

        // The template of the component determines the variables available in its slots, so it is processed first
        if (!_renderMethods.ContainsKey(renderMethodName))
        {
            // The component is rendered in its own method, so only its properties are available
            var loopDepth = _loopDepth;
            _loopDepth = 0;
            var componentBody = ProcessNode(
                templateContentNode,
                componentMap.Overload(component.ImportedComponents),
                CreateComponentContext(component),
                component,
                fallthroughAttributesAliasMap
            );
            _loopDepth = loopDepth;

            _renderMethods.Add(
                renderMethodName,
                CreateComponentRenderMethod(component, renderMethodName, componentBody, fallthroughAttributesAliasMap)
            );
        }

        // Create slot render methods and pass them to the component
        var slotNodes = MtSlotTransformer.GetSlotNodesGroupedBySlotName(componentNode, component);
        foreach (var slotName in component.Slots)
        {
            var slotContext = _slotTransformer.CreateSlotContext(component, slotName, currentContext,
                componentNode.Name);

            var slotContent = slotNodes.TryGetValue(slotName, out var slotNode)
                ? ProcessNode(slotNode, componentMap, slotContext, parentComponent)
                : "";

            _slotTransformer.AddSlotRenderMethod(scope, slotContext, slotName, slotContent, parentComponent);
            componentRenderArguments.Add(MtCodeNames.MethodCallArgument(MtCodeNames.SlotRendererVariable(slotName),
                _slotTransformer.CreateSlotRendererLambda(scope, component, slotName, slotContext, parentComponent)));
        }

        return $"{renderMethodName}({string.Join(", ", componentRenderArguments)});\n";
    }

    /// <summary>
    /// Converts the attributes of a component node to the arguments of its render method. Attributes that are not
    /// properties of the component fall through to its root element, if the template has a single root element.
    /// Their alias names are added to the given map.
    /// </summary>
    private static List<string> CreateComponentArguments(MtComponent component, MtComponentAttributes attributeList,
        XmlNode templateContentNode, Dictionary<string, string> fallthroughAttributesAliasMap)
    {
        var arguments = new List<string>();
        var hasSingleRootElement =
            templateContentNode.ChildNodes.Count == 1 && templateContentNode.FirstChild?.Name != FragmentTag;

        foreach (var (attributeName, attribute) in attributeList)
        {
            bool isStringType;
            string attributeNameAlias;

            if (component.Properties.TryGetValue(attributeName, out var componentProperty))
            {
                isStringType = componentProperty.IsStringType();
                attributeNameAlias = attributeName;
            }
            else if (hasSingleRootElement)
            {
                isStringType = true;
                attributeNameAlias = MtCodeNames.FallthroughAttributeAlias(attributeName);
                fallthroughAttributesAliasMap[attributeName] = attributeNameAlias;
            }
            else
            {
                continue;
            }

            var methodArgument = isStringType
                ? StringUtils.WrapStringInQuotes(
                    CurlyBraceUtils.ReplaceCurlyBraces(attribute.Value, s => $"{{({s})}}"))
                : CurlyBraceUtils.ReplaceCurlyBraces(attribute.Value, s => $"({s})");

            arguments.Add(MtCodeNames.MethodCallArgument(attributeNameAlias, methodArgument));
        }

        return arguments;
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentRenderMethod(MtComponent component, string renderMethodName, string componentBody,
        Dictionary<string, string> aliasMap)
    {
        // open method arguments
        var arguments = new List<string>();
        var body = new StringBuilder(componentBody);

        // add slot render methods
        _slotTransformer.AppendSlotRendererParameters(component, arguments);

        // add component properties as arguments with defaults
        AppendComponentPropertiesArgumentsList(component, arguments);

        // Add fallthrough variables to component render method call
        arguments.AddRange(aliasMap.Values.Select(aliasAttributeName => $"string? {aliasAttributeName} = null"));

        // Insert mania scripts
        if (component.Scripts.Count > 0)
        {
            var scriptRenderMethodName = MtCodeNames.ComponentScriptsRenderMethod(component);
            var scriptRenderMethod = CreateComponentScriptsRenderMethod(component, scriptRenderMethodName);
            _maniaScriptRenderMethods.TryAdd(scriptRenderMethodName, scriptRenderMethod);

            var scriptArguments = new List<string>();

            // add method arguments with defaults
            scriptArguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null)
                .Select(property => property.Name));

            body.AppendLine(maniaTemplateLanguage.FeatureBlockStart())
                .AppendLine(
                    $"__maniaScriptRenderMethods.Add(() => {scriptRenderMethodName}({string.Join(',', scriptArguments)}));")
                .AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
        }

        return maniaTemplateLanguage.RenderMethod(renderMethodName, arguments, body.ToString());
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentScriptsRenderMethod(MtComponent component, string renderMethodName)
    {
        var arguments = new List<string>();

        // Add method arguments with defaults
        AppendComponentPropertiesArgumentsList(component, arguments);

        return maniaTemplateLanguage.RenderMethod(renderMethodName, arguments, _scriptTransformer.CreateManiaScriptBlock(component));
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
    /// Creates the context of a component template, which contains the properties of the component.
    /// </summary>
    private static MtDataContext CreateComponentContext(MtComponent component)
    {
        var context = new MtDataContext();
        foreach (var property in component.Properties.Values)
        {
            context[property.Name] = property.Type;
        }

        return context;
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
            .AppendJoin("\n", _slotTransformer.SlotRenderMethods)
            .ToString();
    }

    /// <summary>
    /// Throws if a fragment has attributes left after pulling conditions and loops, as they can not be rendered.
    /// </summary>
    private static void EnsureFragmentHasNoAttributes(MtComponentAttributes attributeList)
    {
        // Default slot contents may be wrapped in <template slot="default">
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
}
