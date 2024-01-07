using System.CodeDom;
using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;
using Microsoft.CSharp;

namespace ManiaTemplates.Lib;

public class MtTransformer
{
    private readonly ManiaTemplateEngine _engine;
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage;
    private readonly List<string> _namespaces = new();
    private readonly List<MtDataContext> _dataContexts = new();
    private readonly Dictionary<string, MtComponentScript> _maniaScripts = new();
    private readonly Dictionary<string, string> _renderMethods = new();
    private readonly Dictionary<string, string> _maniaScriptRenderMethods = new();
    private readonly List<MtComponentSlot> _slots = new();
    private readonly Dictionary<string, string> _maniaScriptIncludes = new();
    private readonly Dictionary<string, string> _maniaScriptConstants = new();
    private readonly Dictionary<string, string> _maniaScriptStructs = new();
    private int _loopDepth;

    private static readonly Regex TemplateFeatureControlRegex = new(@"#>\s*<#\+");
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    private static readonly Regex JoinScriptBlocksRegex = new(@"(?s)-->.+?<!--");

    private static readonly Regex ManiaScriptIncludeRegex = new(@"#Include\s+""(.+?)""\s+as\s+([_a-zA-Z]+)");
    private static readonly Regex ManiaScriptConstantRegex = new(@"#Const\s+([a-zA-Z_:]+)\s+.+");
    private static readonly Regex ManiaScriptStructRegex = new(@"(?s)#Struct\s+([_a-zA-Z]+)\s*\{.+?\}");

    private static readonly Regex ManiaScriptGlobalVariableRegex =
        new(@"declare ([A-Z][a-zA-Z_\[\]]+) (\w[a-zA-Z0-9_]+);");

    public MtTransformer(ManiaTemplateEngine engine, IManiaTemplateLanguage maniaTemplateLanguage)
    {
        _engine = engine;
        _maniaTemplateLanguage = maniaTemplateLanguage;
    }

    /// <summary>
    /// Creates the target language template, which can be pre-processed for faster rendering.
    /// </summary>
    public string BuildManialink(MtComponent rootComponent, string className, int version = 3)
    {
        _namespaces.AddRange(rootComponent.Namespaces);

        var renderBodyArguments =
            string.Join(',', rootComponent.Slots.Select(slotName => "() => DoNothing()").ToList());
        var rootContext = GetContextFromComponent(rootComponent, "Root");
        var body = ProcessNode(
            XmlStringToNode(rootComponent.TemplateContent),
            _engine.BaseMtComponents.Overload(rootComponent.ImportedComponents),
            rootContext
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
            CreateDataClassesBlock(rootContext),
            CreateInsertedManiaScriptsList(),
            CreateDoNothingMethod(),
            CreateBodyRenderMethod(body, rootContext, rootComponent),
            CreateRenderMethodsBlock()
        };

        return JoinFeatureBlocks(template.ToString());
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateDoNothingMethod()
    {
        return _maniaTemplateLanguage.FeatureBlock(@"string DoNothing(){return """";}").ToString();
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string CreateInsertedManiaScriptsList()
    {
        return new StringBuilder()
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("List<string> __insertedOneTimeManiaScripts = new List<string>();")
            .AppendLine("List<Action> __maniaScriptRenderMethods = new List<Action>();")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtDataContext context, MtComponent rootComponent)
    {
        var methodArguments = new List<string>();
        AppendSlotRenderArgumentsToList(methodArguments, rootComponent);
        var bodyRenderMethod = new StringBuilder($"void RenderBody({string.Join(',', methodArguments)}) {{\n");

        //Arguments for root data context
        var renderBodyArguments =
            $"new {context} {{ {string.Join(",", context.ToList().Select(contextProperty => $"{contextProperty.Key} = {contextProperty.Key}"))} }}";

        //Initialize root data context
        bodyRenderMethod.AppendLine($"var __data = {renderBodyArguments};");

        //Root mania script block
        string rootScriptBlock = "";
        if (rootComponent.Scripts.Count > 0)
        {
            rootScriptBlock = CreateManiaScriptBlock(rootComponent);
        }

        var subManiaScripts = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine(
                "foreach(var maniaScriptRenderMethod in __maniaScriptRenderMethods){ maniaScriptRenderMethod(); }")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();

        //Render content
        bodyRenderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(CreateManiaScriptDirectivesBlock())
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
        string? slotContent = null, MtComponent? parentComponent = null)
    {
        var variablesInherited = new List<string>();
        var methodName = GetSlotRenderMethodName(scope, slotName);

        var methodArguments = new List<string>
        {
            $"{context} __data"
        };
        //add slot render methods
        if (parentComponent != null)
        {
            AppendSlotRenderArgumentsToList(methodArguments, parentComponent);
        }
        else
        {
            AppendSlotRenderArgumentsToList(methodArguments, component);
        }

        var output = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("void " + CreateMethodCall(methodName, string.Join(',', methodArguments), "") + " {");

        if (context.ParentContext != null)
        {
            output.AppendLine(CreateLocalVariablesFromContext(context.ParentContext));
            variablesInherited.AddRange(context.ParentContext.Keys);
        }

        //TODO: add slot render methods of current component?

        output
            .AppendLine(CreateLocalVariablesFromContext(context, variablesInherited))
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(slotContent)
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());

        return output.ToString();
    }

    /// <summary>
    /// Creates a list of variables from a given context, used in render methods.
    /// </summary>
    private static string CreateLocalVariablesFromContext(MtDataContext context,
        ICollection<string>? variablesInherited = null)
    {
        var localVariables = new StringBuilder();

        foreach (var dataName in context.Keys.Where(dataName =>
                     variablesInherited == null || !variablesInherited.Contains(dataName)))
        {
            localVariables.AppendLine($"var {dataName} = __data.{dataName};");
        }

        return localVariables.ToString();
    }

    /// <summary>
    /// Process a ManiaTemplate node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap availableMtComponents, MtDataContext context,
        MtComponent? parentComponent = null)
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
                _dataContexts.Add(forEachCondition.Context);
                _loopDepth++;
            }

            if (availableMtComponents.ContainsKey(tag))
            {
                //Node is a component
                var component = _engine.GetComponent(availableMtComponents[tag].TemplateKey);
                var slotContents =
                    GetSlotContentsBySlotName(childNode, component, availableMtComponents, currentContext);

                var componentRenderMethodCall = ProcessComponentNode(
                    context != currentContext,
                    childNode.GetHashCode(), // = newScope
                    component,
                    currentContext,
                    attributeList,
                    ProcessNode(
                        XmlStringToNode(component.TemplateContent),
                        availableMtComponents.Overload(component.ImportedComponents),
                        currentContext,
                        component
                    ),
                    slotContents,
                    parentComponent
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
                            .AppendLine(CreateMethodCall($"__slotRenderer_{slotName.ToLower()}", ""))
                            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
                        break;

                    default:
                    {
                        var hasChildren = childNode.HasChildNodes;
                        subSnippet.AppendLine(CreateXmlOpeningTag(tag, attributeList, hasChildren));

                        if (hasChildren)
                        {
                            subSnippet.AppendLine(1,
                                ProcessNode(childNode, availableMtComponents, currentContext));
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

    private Dictionary<string, string> GetSlotContentsBySlotName(XmlNode componentNode,
        MtComponent component, MtComponentMap availableMtComponents, MtDataContext context)
    {
        var contentsByName = new Dictionary<string, XmlNode>();

        foreach (XmlNode childNode in componentNode.ChildNodes)
        {
            if (childNode.Name != "template")
            {
                continue;
            }

            var slotName = childNode.Attributes?["slot"]?.Value.ToLower() ?? "default";

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

        // if (componentNode.InnerXml.Trim().Length > 0)
        // {
        contentsByName["default"] = componentNode;
        // }

        return contentsByName.ToDictionary(
            kvp => kvp.Key,
            kvp => ProcessNode(kvp.Value, availableMtComponents, context)
        );
    }

    /// <summary>
    /// Process a node that has been identified as an component.
    /// </summary>
    private string ProcessComponentNode(
        bool newScopeCreated,
        int scope,
        MtComponent component,
        MtDataContext currentContext,
        MtComponentAttributes attributeList,
        string componentBody,
        IReadOnlyDictionary<string, string> slotContents,
        MtComponent? parentComponent = null
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
                Context = currentContext,
                Name = slotName,
                RenderMethodT4 = CreateSlotRenderMethod(component, scope, currentContext, slotName, slotContent,
                    parentComponent)
            });
        }

        var renderMethodName = GetComponentRenderMethodName(component);
        if (!_renderMethods.ContainsKey(renderMethodName))
        {
            _renderMethods.Add(
                renderMethodName,
                CreateComponentRenderMethod(component, renderMethodName, componentBody, currentContext, scope)
            );
        }

        //Create render call
        var renderComponentCall = new StringBuilder(renderMethodName).Append("(__data: ");
        
        if (newScopeCreated)
        {
            var dataVariableSuffix = "(__data)";
            // if (currentContext.ParentContext is { Count: 0 })
            // {
            //     dataVariableSuffix = "";
            // }

            renderComponentCall.Append($"new {currentContext}{dataVariableSuffix}{{");

            var variables = new List<string>();
            foreach (var variableName in currentContext.Keys)
            {
                variables.Add($"{variableName} = {variableName}");
            }

            renderComponentCall.Append(string.Join(", ", variables)).Append("}");
        }
        else
        {
            renderComponentCall.Append("__data");
        }
        
        // renderComponentCall.Append(", ");

        //Pass available arguments
        var renderArguments = new List<string>
        {
            ""
        };

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
                    .Append(GetSlotRenderMethodName(scope, slotName));

                if (newScopeCreated)
                {
                    var dataVariableSuffix = "(__data)";
                    if (currentContext.ParentContext is { Count: 0 })
                    {
                        dataVariableSuffix = "";
                    }

                    renderComponentCall.Append($"(__data: new {currentContext}{dataVariableSuffix}{{");

                    var variables = new List<string>();
                    foreach (var variableName in currentContext.Keys)
                    {
                        variables.Add($"{variableName} = {variableName}");
                    }

                    renderComponentCall.Append(string.Join(", ", variables)).Append("}");
                }
                else
                {
                    renderComponentCall.Append("(__data: __data");
                }

                if (parentComponent != null)
                {
                    foreach (var parentSlotName in parentComponent.Slots)
                    {
                        renderComponentCall.Append(
                            $", __slotRenderer_{parentSlotName}: __slotRenderer_{parentSlotName}");
                    }
                }
                else
                {
                    foreach (var parentSlotName in component.Slots)
                    {
                        renderComponentCall.Append(
                            $", __slotRenderer_{parentSlotName}: () => DoNothing()");
                    }
                }

                renderComponentCall.Append(")");

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
        MtDataContext currentContext, int scope)
    {
        var renderMethod = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .Append("void ")
            .Append(renderMethodName)
            .Append('(');

        //open method arguments
        var arguments = new List<string>
        {
            $"{currentContext} __data"
        };

        //add slot render methods
        AppendSlotRenderArgumentsToList(arguments, component);

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
        renderMethod.AppendLine(componentBody);

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
        renderMethod.AppendLine(CreateManiaScriptBlock(component));

        return renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .Append('}')
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    private string CreateManiaScriptBlock(MtComponent component)
    {
        var renderMethod = new StringBuilder("<script>");

        foreach (var script in component.Scripts)
        {
            if (script.Once)
            {
                renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                    .AppendLine($@"if(!__insertedOneTimeManiaScripts.Contains(""{script.ContentHash()}"")){{")
                    .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
            }

            renderMethod.AppendLine(ReplaceCurlyBraces(ExtractManiaScriptDirectives(script.Content),
                _maniaTemplateLanguage.InsertResult));

            if (script.Once)
            {
                renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                    .AppendLine($@"__insertedOneTimeManiaScripts.Add(""{script.ContentHash()}"");")
                    .AppendLine("}")
                    .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
            }
        }

        return renderMethod.AppendLine("</script>").ToString();
    }

    private string ExtractManiaScriptDirectives(string maniaScriptSource)
    {
        var output = maniaScriptSource;

        var includeMatcher = ManiaScriptIncludeRegex.Match(output);
        while (includeMatcher.Success)
        {
            var match = includeMatcher.ToString();
            var libraryToInclude = includeMatcher.Groups[1].Value;
            var includedAs = includeMatcher.Groups[2].Value;

            if (_maniaScriptIncludes.TryGetValue(includedAs, out var blockingLibrary))
            {
                if (blockingLibrary != libraryToInclude)
                {
                    throw new DuplicateManiaScriptIncludeException(
                        $"Can't include {libraryToInclude} as {includedAs}, because another include ({blockingLibrary}) blocks it.");
                }
            }
            else
            {
                _maniaScriptIncludes.Add(includedAs, libraryToInclude);
            }

            output = output.Replace(match, "");
            includeMatcher = includeMatcher.NextMatch();
        }

        var constantMatcher = ManiaScriptConstantRegex.Match(output);
        while (constantMatcher.Success)
        {
            var match = constantMatcher.ToString();
            _maniaScriptConstants.TryAdd(constantMatcher.Groups[1].Value, match);
            output = output.Replace(match, "");
            //TODO: exceptions double constant

            constantMatcher = constantMatcher.NextMatch();
        }

        var structMatcher = ManiaScriptStructRegex.Match(output);
        while (structMatcher.Success)
        {
            var structName = structMatcher.Groups[1].Value;
            var structDefinition = structMatcher.ToString().Trim();

            if (_maniaScriptStructs.TryGetValue(structName, out var existingStructDefinition))
            {
                if (string.Compare(structDefinition, existingStructDefinition, CultureInfo.CurrentCulture,
                        CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols) != 0)
                {
                    throw new DuplicateManiaScriptStructException(
                        $"Can't redefine struct {structName}, because it is already defined as: {existingStructDefinition}.");
                }
            }
            else
            {
                _maniaScriptStructs.Add(structName, structDefinition);
            }

            output = output.Replace(structDefinition, "");

            structMatcher = structMatcher.NextMatch();
        }

        return output;
    }

    private string CreateManiaScriptDirectivesBlock()
    {
        if (_maniaScriptIncludes.Count + _maniaScriptStructs.Count + _maniaScriptConstants.Count == 0)
        {
            return "";
        }

        var output = new StringBuilder("<script><!--\n");

        output.AppendJoin("\n",
                _maniaScriptIncludes.Select(include => $@"#Include ""{include.Value}"" as {include.Key}"))
            .AppendLine()
            .AppendJoin("\n", _maniaScriptStructs.Values)
            .AppendLine()
            .AppendJoin("\n", _maniaScriptConstants.Values);

        return output.AppendLine("\n--></script>").ToString();
    }

    /// <summary>
    /// Checks the attribute list for if-condition and returns it, else null.
    /// </summary>
    private string? GetIfConditionFromNodeAttributes(MtComponentAttributes attributeList)
    {
        return attributeList.ContainsKey("if") ? attributeList.Pull("if") : null;
    }

    /// <summary>
    /// Checks the attribute list for name and returns it, else "default".
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
    /// Creates a list of class definitions for all data contexts.
    /// </summary>
    private string CreateDataClassesBlock(MtDataContext rootContext)
    {
        var dataClasses = new List<string>
        {
            CreateContextClass(rootContext)
        };

        dataClasses.AddRange(_dataContexts.Select(dataContext => CreateContextClass(dataContext, rootContext)));

        return "\n" + _maniaTemplateLanguage.FeatureBlock(string.Join("\n\n", dataClasses)).ToString();
    }

    /// <summary>
    /// Creates a single class definition for the given data context and optional root context.
    /// </summary>
    private static string CreateContextClass(MtDataContext context, MtDataContext? rootContext = null)
    {
        var dataClass = new StringBuilder();

        //Name of the data context class
        var className = context.ToString();

        //Extend previous context
        if (context.ParentContext != null)
        {
            className += $": {context.ParentContext}";
        }

        //Create body of context class
        dataClass.AppendLine($"class {className} {{");
        dataClass.AppendLine(CreateContextClassProperties(context));
        dataClass.AppendLine(CreateContextClassConstructor(context, context.ParentContext == rootContext));
        dataClass.AppendLine("}");

        return dataClass.ToString();
    }

    /// <summary>
    /// Creates the constructor for a data context class.
    /// </summary>
    private static string CreateContextClassConstructor(MtDataContext context, bool parentContextIsRoot)
    {
        if (context.ParentContext is not { Count: > 0 }) return "";

        var dataClass = new StringBuilder();
        dataClass.AppendLine(parentContextIsRoot
            ? $"internal {context}({context.ParentContext} data) {{"
            : $"internal {context}({context.ParentContext} data) : base(data) {{");

        foreach (var propertyName in context.ParentContext.Keys)
        {
            dataClass.AppendLine($"{propertyName} = data.{propertyName};");
        }

        dataClass.AppendLine("}");

        return dataClass.ToString();
    }

    /// <summary>
    /// Creates the properties of a data context class.
    /// </summary>
    private static string CreateContextClassProperties(MtDataContext context)
    {
        return string.Join("\n",
            context.Select(property => $"public {property.Value} {property.Key} {{ get; set; }}").ToList());
    }

    /// <summary>
    /// Parses the attributes of a XmlNode to an MtComponentAttributes-instance.
    /// </summary>
    private static MtComponentAttributes GetXmlNodeAttributes(XmlNode node)
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
    /// Creates a method which renders all loaded ManiaScripts into a block, which is usually placed at the end of the ManiaLink.
    /// </summary>
    private string BuildManiaScripts(MtComponent rootComponent)
    {
        var maniaScripts = rootComponent.Scripts.ToDictionary(script => script.ContentHash());
        foreach (var (key, value) in _maniaScripts)
        {
            maniaScripts[key] = value;
        }

        MtComponentScript? mainScript = null;
        var scripts = new StringBuilder();
        var scriptMethod = new Snippet
        {
            "void RenderManiaScripts() {",
        };

        var maniaScriptsByDepth = from script in maniaScripts orderby script.Value.Depth descending select script;
        foreach (var (i, script) in maniaScriptsByDepth)
        {
            if (script.HasMainMethod)
            {
                if (mainScript != null)
                {
                    throw new DuplicateMainManiaScriptException(
                        "You may only include one main-method per ManiaLink. Offending script:\n" + mainScript.Content);
                }

                mainScript = script;
            }

            scripts.AppendLine(script.Content);
        }

        var joinedScripts = string.Join("\n", scripts);
        while (JoinScriptBlocksRegex.IsMatch(joinedScripts))
        {
            joinedScripts = JoinScriptBlocksRegex.Replace(joinedScripts, "");
        }

        scriptMethod.AppendLine("#>")
            .AppendLine("<script>")
            .AppendLine(joinedScripts)
            .AppendLine("</script>")
            .AppendLine("<#+")
            .AppendLine("}");

        return _maniaTemplateLanguage.FeatureBlock(scriptMethod.ToString()).ToString();
    }

    /// <summary>
    /// Creates a fresh data context from a component instance.
    /// </summary>
    private static MtDataContext GetContextFromComponent(MtComponent component, string? name = null)
    {
        var context = new MtDataContext(name);

        foreach (var property in component.Properties.Values)
        {
            context.Add(property.Name, property.Type);
        }

        return context;
    }

    /// <summary>
    /// Returns C# code representation of the type.
    /// </summary>
    /// <param name="type">The type.</param>
    private static string GetFormattedName(Type type)
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
    private static string CreateMethodCall(string methodName, string methodArguments = "__data",
        string append = ";")
    {
        return $"{methodName}({methodArguments}){append}";
    }

    /// <summary>
    /// Returns the method name that renders the given component.
    /// </summary>
    private string GetComponentRenderMethodName(MtComponent component)
    {
        return $"Render_Component_{component.Id()}";
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
    private static string WrapStringInQuotes(string str)
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
    private static string ManiaLinkStart(string name, int version = 3, string? displayLayer = null)
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
    private string CreateXmlOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren)
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
    private static XmlNode XmlStringToNode(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }

    /// <summary>
    /// Takes the contents of double curly braces in a string and wraps them into something else. The second Argument takes a string-argument and returns the newly wrapped string.
    /// </summary>
    private static string ReplaceCurlyBraces(string value, Func<string, string> curlyContentWrapper)
    {
        var matches = TemplateInterpolationRegex.Match(value);
        var output = value;

        while (matches.Success)
        {
            var match = matches.Groups[0].Value.Trim();
            var content = matches.Groups[1].Value.Trim();

            output = output.Replace(match, curlyContentWrapper(content));

            matches = matches.NextMatch();
        }

        return output;
    }

    /// <summary>
    /// Joins consecutive feature blocks to reduce generated code.
    /// </summary>
    private static string JoinFeatureBlocks(string manialink)
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