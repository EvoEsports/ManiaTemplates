﻿using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

public class Transformer
{
    private readonly ManiaTemplateEngine _engine;
    private readonly IManiaTemplateLanguage _maniaTemplateLanguage;
    private readonly Dictionary<string, Snippet> _renderMethods = new();
    private readonly List<string> _namespaces = new();
    private readonly List<MtComponent> _usedComponents = new();
    private readonly List<MtComponentSlot> _slots = new();
    private readonly List<MtComponentNode> _componentNodes = new();

    private static readonly Regex TemplateControlRegex = new(@"#>\s*<#\+");
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");

    public Transformer(ManiaTemplateEngine engine, IManiaTemplateLanguage maniaTemplateLanguage)
    {
        _engine = engine;
        _maniaTemplateLanguage = maniaTemplateLanguage;
    }

    /// <summary>
    /// Creates the target language template, which can be pre-processed for faster rendering.
    /// </summary>
    public string BuildManialink(MtComponent component, string className, int version = 3)
    {
        _namespaces.AddRange(component.Namespaces);

        var loadedComponents = _engine.BaseMtComponents.Overload(component.ImportedComponents);
        var maniaScripts = new Dictionary<int, MtComponentScript>();

        //quick fix
        foreach (var script in component.Scripts)
        {
            maniaScripts.Add(script.ContentHash(), script);
        }

        var rootContext = GetContextFromComponent(component);

        var body = ProcessNode(
            XmlStringToNode(component.TemplateContent),
            loadedComponents,
            maniaScripts,
            component,
            rootContext
        );

        var renderBodyArguments =
            $"new {rootContext.ToString()}{{ {string.Join(",", rootContext.ToList().Select(contextProperty => $"{contextProperty.Key} = {contextProperty.Key}"))} }}";

        var template = new Snippet
        {
            _maniaTemplateLanguage.Context(@"template language=""C#"""), //Might not be needed
            _maniaTemplateLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            CreateImportStatements(),
            ManiaLinkStart(className, version),
            _maniaTemplateLanguage.Code($"RenderBody({renderBodyArguments});"),
            _maniaTemplateLanguage.Code($"RenderManiaScripts({renderBodyArguments});"),
            ManiaLinkEnd(),
            CreateTemplateParametersPreCompiled(component),
            CreateDataClassesBlock(component, rootContext),
            CreateBodyRenderMethod(body, rootContext),
            CreateRenderAndDataMethods(),
            BuildManiaScripts(maniaScripts, rootContext)
        };

        return JoinFeatureBlocks(template.ToString());
    }

    private MtComponentContext GetContextFromComponent(MtComponent component)
    {
        var context = new MtComponentContext();

        foreach (var property in component.Properties.Values)
        {
            context.Add(property.Name, property.Type);
        }

        return context;
    }

    /// <summary>
    /// Creates the method that renders the body of the ManiaLink.
    /// </summary>
    private string CreateBodyRenderMethod(string body, MtComponentContext context)
    {
        var bodyRenderMethod = new StringBuilder()
            .AppendLine($"void RenderBody({context.ToString()} __data){{")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
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
        var snippet = new Snippet();

        foreach (var ns in _namespaces)
        {
            snippet.AppendLine(_maniaTemplateLanguage.Context($@"import namespace=""{ns}"""));
        }

        return snippet.ToString();
    }

    /// <summary>
    /// Joins consecutive feature blocks to reduce generated code.
    /// </summary>
    private static string JoinFeatureBlocks(string manialink)
    {
        var match = TemplateControlRegex.Match(manialink);
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

    /// <summary>
    /// Creates a block containing the properties of the template for the target language, which are filled when rendering.
    /// </summary>
    private string CreateTemplateParametersPreCompiled(MtComponent mtComponent)
    {
        var snippet = new Snippet();

        foreach (var (propertyName, property) in mtComponent.Properties)
        {
            snippet.AppendSnippet(_maniaTemplateLanguage.FeatureBlock(GetClassParameter(property)));
        }

        return snippet.ToString();
    }

    private string GetClassParameter(MtComponentProperty property)
    {
        return $"public {property.Type} {property.Name} {{ get; init; }}";
    }

    /// <summary>
    /// Creates a block containing all render methods used in the template.
    /// </summary>
    private string CreateRenderAndDataMethods()
    {
        Snippet snippet = new();

        foreach (var renderMethod in _renderMethods.Values)
        {
            snippet.AppendSnippet(renderMethod);
        }

        var createdMethods = new List<string>();
        foreach (var slot in _slots)
        {
            var context = slot.Context;
            if (slot.Context.ParentContext != null)
            {
                context = slot.Context.ParentContext;
            }

            var renderContextId = context.ToString();
            if (createdMethods.Contains(renderContextId))
            {
                continue;
            }

            snippet.AppendLine(CreateSlotRenderMethods(slot, context));
            createdMethods.Add(renderContextId);
        }

        return snippet.ToString();
    }

    private string CreateSlotRenderMethods(MtComponentSlot slot, MtComponentContext context)
    {
        var output = new StringBuilder();

        var arguments = $"{context.ToString()} __data";

        output.AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("void " + CreateMethodCall(slot.RenderMethodName(context.ToString()), arguments, ""))
            .AppendLine("{")
            .AppendLine(CreateLocalVariables(context).ToString())
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(slot.Content)
            .AppendLine(_maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine("}")
            .AppendLine(_maniaTemplateLanguage.FeatureBlockEnd());

        return output.ToString();
    }

    /// <summary>
    /// Creates the render method for the given component node, which contains all markup/component-calls for that specific node.
    /// </summary>
    private Snippet CreateRenderMethod(MtComponentNode componentNode)
    {
        var methodBody = new Snippet()
            .AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(componentNode.TemplateContent)
            .AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());

        var renderContextId = componentNode.Context.ToString();
        var arguments = $"{renderContextId} __data";
        if (componentNode.HasSlot && componentNode.Context.ParentContext != null)
        {
            arguments += $", {componentNode.Context.ParentContext} __parentData";
        }

        var localVariables = new Snippet();
        var localVariablesCreated = new List<string>();

        foreach (var dataName in componentNode.Context.Keys)
        {
            localVariables.AppendLine($"var {dataName} = __data.{dataName};");
            localVariablesCreated.Add(dataName);
        }

        if (componentNode.Context.ParentContext != null)
        {
            var inheritedVariables = componentNode.Context.ParentContext.Keys
                .Where(dataName => !localVariablesCreated.Contains(dataName));

            foreach (var dataName in inheritedVariables)
            {
                localVariables.AppendLine($"var {dataName} = __data.{dataName};");
            }
        }

        var renderMethod = new Snippet()
            .AppendLine($"void {GetRenderMethodName(componentNode)}({arguments})")
            .AppendLine("{")
            .AppendSnippet(1, localVariables)
            .AppendSnippet(1, methodBody)
            .AppendLine("}")
            .AppendLine("")
            .ToString();

        return _maniaTemplateLanguage.FeatureBlock(renderMethod);
    }

    private Snippet CreateLocalVariables(MtComponentContext context)
    {
        var localVariables = new Snippet();
        foreach (var dataName in context.Keys)
        {
            localVariables.AppendLine($"var {dataName} = __data.{dataName};");
        }

        return localVariables;
    }

    /// <summary>
    /// Creates a method block for the target language.
    /// </summary>
    private Snippet CreateMethodBlock(string returnType, string methodName, string arguments, Snippet body)
    {
        var methodBlock = new Snippet(1)
            .AppendLine($"{returnType} {methodName}({arguments})")
            .AppendLine("{")
            .AppendSnippet(body)
            .AppendLine("}")
            .ToString();

        return _maniaTemplateLanguage.FeatureBlock(methodBlock);
    }

    /// <summary>
    /// Takes all set arguments of a component node and converts them to a string, which contains the arguments for the render method.
    /// </summary>
    private string ComponentNodeToMethodArguments(MtComponentNode mtComponentNode)
    {
        Snippet propertyAssignments = new();

        foreach (var property in mtComponentNode.MtComponent.Properties.Values)
        {
            if (!mtComponentNode.Attributes.Has(property.Name))
            {
                continue;
            }

            var value = mtComponentNode.Attributes.Get(property.Name);
            propertyAssignments.AppendLine(@$"{property.Name} = {ConvertPropertyAssignment(property, value)}");
        }

        return propertyAssignments.ToString(", ");
    }

    /// <summary>
    /// Process a ManiaTemplate node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap availableMtComponents,
        Dictionary<int, MtComponentScript> maniaScripts, MtComponent parentComponent, MtComponentContext context,
        MtComponentSlot? slot = null)
    {
        //TODO: slim down method/outsource code from method
        Snippet snippet = new();

        foreach (XmlNode child in node.ChildNodes)
        {
            var tag = child.Name;
            var attributeList = GetNodeAttributes(child);

            string? forEachLoop = null;
            if (attributeList.Has("foreach"))
            {
                forEachLoop = attributeList.Pull("foreach");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " int __index = 0;");
                snippet.AppendLine(null, $" foreach({forEachLoop})");
                snippet.AppendLine(null, " {");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
            }

            string? ifStatement = null;
            if (attributeList.Has("if"))
            {
                ifStatement = attributeList.Pull("if");
                string ifContent = TemplateInterpolationRegex.Replace(ifStatement, "$1");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
                snippet.AppendLine(null, $" if({ifContent})");
                snippet.AppendLine(null, " {");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
            }

            if (availableMtComponents.ContainsKey(tag))
            {
                var component = _engine.GetComponent(availableMtComponents[tag].TemplateKey);
                foreach (var ns in component.Namespaces)
                {
                    _namespaces.Add(ns);
                }

                if (!_usedComponents.Contains(component))
                {
                    _usedComponents.Add(component);
                }

                foreach (var script in component.Scripts)
                {
                    var scriptHash = script.ContentHash();
                    if (maniaScripts.ContainsKey(scriptHash))
                    {
                        continue;
                    }

                    maniaScripts.Add(scriptHash, script);
                }

                var componentNode = CreateComponentNode(
                    currentNode: child,
                    component: component,
                    availableMtComponents: availableMtComponents,
                    attributeList: attributeList,
                    slot: slot,
                    maniaScripts: maniaScripts,
                    parentComponent: parentComponent,
                    context: context.NewContext(GetContextFromComponent(component))
                );
                _componentNodes.Add(componentNode);

                snippet.AppendLine(CreateComponentRenderMethodCall(componentNode));

                var renderMethodName = GetRenderMethodName(componentNode);
                if (!_renderMethods.ContainsKey(renderMethodName))
                {
                    _renderMethods.Add(renderMethodName, CreateRenderMethod(componentNode));
                }
            }
            else
            {
                switch (tag)
                {
                    case "#text":
                        snippet.AppendLine(child.InnerText);
                        break;
                    case "#comment":
                        snippet.AppendLine($"<!-- {child.InnerText} -->");
                        break;
                    case "slot":
                        if (slot != null)
                        {
                            var renderContextId = context.ParentContext != null
                                ? context.ParentContext.ToString()
                                : context.ToString();
                            snippet.AppendLine(
                                $"<#+ {CreateMethodCall(slot.RenderMethodName(renderContextId), "__parentData")} #>");
                        }
                        else
                        {
                            throw new Exception("No parent to provide content to slot.");
                        }

                        break;
                    default:
                    {
                        var hasChildren = child.HasChildNodes;
                        snippet.AppendLine(CreateXmlOpeningTag(tag, attributeList, hasChildren));

                        if (hasChildren)
                        {
                            snippet.AppendLine(1,
                                ProcessNode(child, availableMtComponents, maniaScripts, parentComponent, context,
                                    slot));
                            snippet.AppendLine(CreateXmlClosingTag(tag));
                        }

                        break;
                    }
                }
            }

            if (ifStatement != null)
            {
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " }");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
            }

            if (forEachLoop != null)
            {
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " __index++;");
                snippet.AppendLine(null, " }");
                snippet.AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd());
            }
        }

        return snippet.ToString();
    }


    private string CreateDataClassesBlock(MtComponent rootComponent, MtComponentContext rootContext)
    {
        var dataClasses = new List<string>
        {
            CreateContextClass(rootContext)
        };

        foreach (var componentNode in _componentNodes)
        {
            dataClasses.Add(CreateContextClass(componentNode.Context, componentNode.MtComponent));
        }

        foreach (var slot in _slots)
        {
            // dataClasses.Add(CreateContextClass(slot.Context));
        }

        return "\n" + _maniaTemplateLanguage.FeatureBlock(string.Join("\n\n", dataClasses)).ToString();
    }

    private string CreateContextClass(MtComponentContext context, MtComponent? component = null)
    {
        var dataClass = new Snippet();
        var className = context.ToString();

        if (context.ParentContext != null)
        {
            className += $": {context.ParentContext.ToString()}";
        }

        dataClass.AppendLine($"class {className} {{");

        foreach (var (dataName, dataType) in context)
        {
            var suffix = "";

            if (component != null && component.Properties.ContainsKey(dataName))
            {
                var property = component.Properties[dataName];
                if (property.Default != null)
                {
                    var defaultValue = WrapIfString(property, property.Default);
                    suffix += $" = {defaultValue};";
                }
            }

            dataClass.AppendLine(1, $"public {dataType} {dataName} {{ get; init; }}{suffix}");
        }

        dataClass.AppendLine("}");

        return dataClass.ToString();
    }

    private string GetComponentDataClassName(MtComponent component)
    {
        return $"Component{component.GetHashCode().ToString().Replace("-", "N")}Data";
    }

    /// <summary>
    /// Parses the attributes of a XmlNode to an MtComponentAttributes-instance.
    /// </summary>
    private static MtComponentAttributes GetNodeAttributes(XmlNode node)
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
    /// Takes parsed information and creates a new MtComponentNode-instance.
    /// </summary>
    private MtComponentNode CreateComponentNode(XmlNode currentNode, MtComponent component,
        MtComponentMap availableMtComponents, MtComponentAttributes attributeList, MtComponentSlot? slot,
        Dictionary<int, MtComponentScript> maniaScripts, MtComponent parentComponent, MtComponentContext context)
    {
        var subComponents = availableMtComponents.Overload(component.ImportedComponents);
        var subSlot = new MtComponentSlot
        {
            Component = component,
            ParentComponent = parentComponent,
            Context = context,
            Content = ProcessNode(
                currentNode,
                availableMtComponents,
                maniaScripts,
                parentComponent,
                context,
                slot
            )
        };

        var componentTemplate = ProcessNode(
            XmlStringToNode(component.TemplateContent),
            subComponents,
            maniaScripts,
            component,
            context,
            subSlot
        );

        _slots.Add(subSlot);

        return new MtComponentNode(
            currentNode.Name,
            component,
            attributeList,
            componentTemplate,
            Helper.UsesComponents(currentNode, availableMtComponents),
            parentComponent,
            context
        );
    }

    /// <summary>
    /// Used to convert component arguments into expressions, that can be assigned to C# method calls.
    /// </summary>
    private static string ConvertPropertyAssignment(MtComponentProperty property, string parameterValue)
    {
        if (IsStringType(property))
        {
            return "$" + WrapIfString(property, ReplaceCurlyBraces(parameterValue, s => "{" + s + "}"));
        }

        return ReplaceCurlyBraces(parameterValue, s => s);
    }

    /// <summary>
    /// Creates a method which renders all loaded ManiaScripts into a block, which is usually placed at the end of the ManiaLink.
    /// </summary>
    private string BuildManiaScripts(Dictionary<int, MtComponentScript> scripts, MtComponentContext context)
    {
        var scriptMethod = new Snippet
        {
            $"void RenderManiaScripts({context.ToString()} __data)",
            "{",
        };

        foreach (var dataName in context.Keys)
        {
            scriptMethod.AppendLine($"var {dataName} = __data.{dataName};");
        }

        scriptMethod.AppendLine("#>")
            .AppendLine("<script><!--");

        foreach (var script in scripts.Values)
        {
            scriptMethod.AppendLine(script.Content);
        }

        scriptMethod.AppendLine("--></script>");
        scriptMethod.AppendLine("<#+");
        scriptMethod.AppendLine("}");

        return _maniaTemplateLanguage.FeatureBlock(scriptMethod.ToString()).ToString();
    }

    /// <summary>
    /// Creates a method call in the target language, which renders a component.
    /// </summary>
    private string CreateComponentRenderMethodCall(MtComponentNode componentNode)
    {
        var methodName = GetRenderMethodName(componentNode);
        var renderContextId = componentNode.Context.ToString();
        var methodArguments = new StringBuilder();

        methodArguments.Append($"new {renderContextId} {{ ");
        methodArguments.Append(ComponentNodeToMethodArguments(componentNode))
            .Append(" }");

        if (componentNode.MtComponent.HasSlot)
        {
            methodArguments.Append(", __data");
        }

        return _maniaTemplateLanguage.FeatureBlock(CreateMethodCall(methodName, methodArguments.ToString()))
            .ToString(" ");
    }

    /// <summary>
    /// Takes the parsed &lt;property&gt; nodes from the ManiaTemplate and converts them to method arguments.
    /// </summary>
    private string DataToArguments(MtComponent mtComponent)
    {
        var snippet = new Snippet();

        foreach (var property in mtComponent.Properties.Values)
        {
            if (property.Default != null)
            {
                var defaultValue = WrapIfString(property, GetPropertyDefaultValue(property));
                snippet.AppendLine(@$"{property.Type} {property.Name} = {defaultValue}");
            }
            else
            {
                snippet.AppendLine(@$"{property.Type} {property.Name}");
            }
        }

        return snippet.ToString(", ");
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
    /// Returns the default value for a component property.
    /// </summary>
    private static string GetPropertyDefaultValue(MtComponentProperty property)
    {
        if (property.Default != null)
        {
            return property.Default;
        }

        return property.Type.EndsWith('?') ? "null" : $"new {property.Type}()";
    }

    /// <summary>
    /// Returns the name of the render method for a component node.
    /// </summary>
    private static string GetRenderMethodName(MtComponentNode componentNode)
    {
        var renderContextId = componentNode.Context.ToString();
        return $"Render{componentNode.Tag}_{renderContextId}";
    }

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    private static string WrapStringInQuotes(string str)
    {
        return $@"""{str}""";
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

        foreach (var (attributeName, attributeValue) in attributeList.All())
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
}