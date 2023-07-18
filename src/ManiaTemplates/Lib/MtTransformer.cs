using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

public class MtTransformer
{
    private readonly ManiaTemplateEngine _engine;
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage;
    private readonly List<string> _namespaces = new();
    private readonly List<MtDataContext> _dataContexts = new();
    private readonly Dictionary<int, MtComponentScript> _maniaScripts = new();
    private readonly Dictionary<string, string> _renderMethods = new();
    private readonly List<MtComponentSlot> _slots = new();
    private int _loopDepth;

    private static readonly Regex TemplateFeatureControlRegex = new(@"#>\s*<#\+");
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");

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
            ManiaLinkStart(className, version),
            "<#",
            "RenderBody(() => DoNothing());",
            "RenderManiaScripts();",
            "#>",
            ManiaLinkEnd(),
            CreateTemplatePropertiesBlock(rootComponent),
            CreateDataClassesBlock(rootContext),
            DoNothingMethod(),
            CreateBodyRenderMethod(body, rootContext),
            CreateRenderMethodsBlock(),
            BuildManiaScripts(rootComponent)
        };

        return JoinFeatureBlocks(template.ToString());
    }

    /// <summary>
    /// Creates a dummy DoNothing() method that returns a empty string.
    /// </summary>
    private string DoNothingMethod()
    {
        return _maniaTemplateLanguage.FeatureBlock(@"string DoNothing(){return """";}").ToString();
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtDataContext context)
    {
        var bodyRenderMethod = new StringBuilder("void RenderBody(Action __slotRenderer) {\n");

        //Arguments for root data context
        var renderBodyArguments =
            $"new {context} {{ {string.Join(",", context.ToList().Select(contextProperty => $"{contextProperty.Key} = {contextProperty.Key}"))} }}";

        //Initialize root data context
        bodyRenderMethod.AppendLine($"var __data = {renderBodyArguments};");

        //Render content
        bodyRenderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(body)
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}");

        return _maniaTemplateLanguage.FeatureBlock(bodyRenderMethod.ToString()).ToString();
    }

    /// <summary>
    /// Takes all loaded namespaces and creates the import statements for the target language.
    /// </summary>
    private string CreateImportStatements()
    {
        return string.Join("\n",
            _namespaces.Select(nameSpace => _maniaTemplateLanguage.Context($@"import namespace=""{nameSpace}"""))
                .ToList());
    }

    /// <summary>
    /// Creates a block containing the templates properties, which are filled when rendered.
    /// </summary>
    private string CreateTemplatePropertiesBlock(MtComponent mtComponent)
    {
        return string.Join("\n",
            mtComponent.Properties.Values.Select(property =>
                _maniaTemplateLanguage.FeatureBlock($"public {property.Type} {property.Name} {{ get; init; }}")
                    .ToString()));
    }

    /// <summary>
    /// Creates a block containing all render methods used in the template.
    /// </summary>
    private string CreateRenderMethodsBlock()
    {
        return new StringBuilder()
            .AppendJoin("\n", _renderMethods.Values)
            .AppendJoin("\n", _slots.Select(slot => slot.RenderMethod))
            .ToString();
    }

    /// <summary>
    /// Creates the slot-render method for a given data context.
    /// </summary>
    private string CreateSlotRenderMethod(int scope, MtDataContext context, string? slotContent = null)
    {
        var variablesInherited = new List<string>();
        var methodName = "Render_Slot_" + scope;

        var output = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("void " + CreateMethodCall(methodName, $"{context} __data", "") + " {");

        if (context.ParentContext != null)
        {
            output.AppendLine(CreateLocalVariablesFromContext(context.ParentContext));
            variablesInherited.AddRange(context.ParentContext.Keys);
        }

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
    private static string CreateLocalVariablesFromContext(MtDataContext context, ICollection<string>? variablesInherited = null)
    {
        var localVariables = new StringBuilder();
        
        foreach (var dataName in context.Keys.Where(dataName => variablesInherited == null || !variablesInherited.Contains(dataName)))
        {
            localVariables.AppendLine($"var {dataName} = __data.{dataName};");
        }

        return localVariables.ToString();
    }

    /// <summary>
    /// Process a ManiaTemplate node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap availableMtComponents, MtDataContext context)
    {
        Snippet snippet = new();

        var nodeId = 1;
        foreach (XmlNode child in node.ChildNodes)
        {
            var subSnippet = new Snippet();
            var tag = child.Name;
            var attributeList = GetXmlNodeAttributes(child);
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

                string? slotContent = null;
                if (component.HasSlot)
                {
                    slotContent = ProcessNode(
                        child,
                        availableMtComponents,
                        currentContext
                    );
                }

                var componentRenderMethodCall = ProcessComponentNode(
                    context != currentContext,
                    child.GetHashCode(),
                    component,
                    currentContext,
                    attributeList,
                    ProcessNode(
                        XmlStringToNode(component.TemplateContent),
                        availableMtComponents.Overload(component.ImportedComponents),
                        currentContext
                    ),
                    slotContent
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
                        subSnippet.AppendLine(child.InnerText);
                        break;
                    case "#comment":
                        subSnippet.AppendLine($"<!-- {child.InnerText} -->");
                        break;
                    case "slot":
                        subSnippet.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
                            .AppendLine(CreateMethodCall("__slotRenderer", ""))
                            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());
                        break;

                    default:
                    {
                        var hasChildren = child.HasChildNodes;
                        subSnippet.AppendLine(CreateXmlOpeningTag(tag, attributeList, hasChildren));

                        if (hasChildren)
                        {
                            subSnippet.AppendLine(1,
                                ProcessNode(child, availableMtComponents, currentContext));
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
    /// Process a node that has been identified as an component.
    /// </summary>
    private string ProcessComponentNode(
        bool newScopeCreated,
        int scope,
        MtComponent component,
        MtDataContext currentContext,
        MtComponentAttributes attributeList,
        string componentBody,
        string? slotContent = null
    )
    {
        MtComponentSlot? slot = null;
        if (component.HasSlot)
        {
            slot = new MtComponentSlot
            {
                Scope = scope,
                Context = currentContext,
                RenderMethod = CreateSlotRenderMethod(scope, currentContext, slotContent)
            };

            _slots.Add(slot);
        }

        var renderMethodName = GetComponentRenderMethodName(component);
        if (!_renderMethods.ContainsKey(renderMethodName))
        {
            _renderMethods.Add(
                renderMethodName,
                CreateComponentRenderMethod(component, renderMethodName, componentBody)
            );
        }

        //create render call
        var renderComponentCall = new StringBuilder(renderMethodName).Append('(');
        var renderArguments = new List<string>();
        foreach (var (attributeName, attributeValue) in attributeList)
        {
            if (component.Properties.TryGetValue(attributeName, out var value))
            {
                renderArguments.Add(
                    $"{attributeName}: {WrapIfString(value, ReplaceCurlyBraces(attributeValue, s => IsStringType(value) ? $@"{{{s}}}" : s))}");
            }
        }

        renderComponentCall.Append(string.Join(", ", renderArguments));

        if (slot != null)
        {
            if (renderArguments.Count > 0)
            {
                renderComponentCall.Append(", ");
            }

            renderComponentCall.Append("__slotRenderer: ")
                .Append("() => ")
                .Append(GetSlotRenderMethodName(slot));

            if (newScopeCreated)
            {
                var dataVariableSuffix = "(__data)";
                if (currentContext.ParentContext is { Count: 0 })
                {
                    dataVariableSuffix = "";
                }

                renderComponentCall.Append($"(new {currentContext}{dataVariableSuffix}{{");

                var variables = new List<string>();
                foreach (var variableName in currentContext.Keys)
                {
                    variables.Add($"{variableName} = {variableName}");
                }

                renderComponentCall.Append(string.Join(", ", variables)).Append("})");
            }
            else
            {
                renderComponentCall.Append("(__data)");
            }
        }

        renderComponentCall.AppendLine(");");

        _namespaces.AddRange(component.Namespaces);

        foreach (var script in component.Scripts)
        {
            var scriptHash = script.ContentHash();
            if (_maniaScripts.ContainsKey(scriptHash))
            {
                continue;
            }

            _maniaScripts.Add(scriptHash, script);
        }

        return renderComponentCall.ToString();
    }

    /// <summary>
    /// Creates the method which renders the contents of a component.
    /// </summary>
    private string CreateComponentRenderMethod(MtComponent component, string renderMethodName, string componentBody)
    {
        var renderMethod = new StringBuilder(_maniaTemplateLanguage.FeatureBlockStart())
            .Append("void ")
            .Append(renderMethodName)
            .Append('(');

        //open method arguments
        var arguments = new List<string>();

        //add slot render method
        if (component.HasSlot)
        {
            arguments.Add("Action __slotRenderer");
        }

        //add method arguments with defaults
        arguments.AddRange(component.Properties.Values.OrderBy(property => property.Default != null).Select(property =>
            property.Default == null
                ? $"{property.Type} {property.Name}"
                : $"{property.Type} {property.Name} = {WrapIfString(property, property.Default)}"));

        //close method arguments
        renderMethod.Append(string.Join(", ", arguments))
            .AppendLine(") {")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());

        //insert body
        renderMethod.AppendLine(componentBody);

        return renderMethod.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .Append('}')
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Checks the attribute list for if-condition and returns it, else null.
    /// </summary>
    private string? GetIfConditionFromNodeAttributes(MtComponentAttributes attributeList)
    {
        return attributeList.ContainsKey("if") ? attributeList.Pull("if") : null;
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
        
        var scriptMethod = new Snippet
        {
            "void RenderManiaScripts() {",
        };

        scriptMethod.AppendLine("#>")
            .AppendLine("<script><!--");

        foreach (var script in maniaScripts.Values)
        {
            scriptMethod.AppendLine(script.Content);
        }

        scriptMethod.AppendLine("--></script>");
        scriptMethod.AppendLine("<#+");
        scriptMethod.AppendLine("}");

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
    /// Returns the name of the method that renders the slot contents.
    /// </summary>
    private string GetSlotRenderMethodName(MtComponentSlot slot)
    {
        return "Render_Slot_" + slot.Scope.GetHashCode();
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
    private static string ManiaLinkStart(string name, int version = 3)
    {
        return $@"<manialink version=""{version}"" id=""{name}"" name=""EvoSC#-{name}"">";
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
