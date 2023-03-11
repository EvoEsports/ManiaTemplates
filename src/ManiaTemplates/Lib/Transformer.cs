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
    public string BuildManialink(MtComponent mtComponent, string className, int version = 3)
    {
        var loadedComponents = _engine.BaseMtComponents.Overload(mtComponent.ImportedComponents);
        var maniaScripts = new Dictionary<int, MtComponentScript>();
        _namespaces.AddRange(mtComponent.Namespaces);
        var body = ProcessNode(XmlStringToNode(mtComponent.TemplateContent), loadedComponents, maniaScripts, null,
            true);
        var template = new Snippet
        {
            _maniaTemplateLanguage.Context(@"template language=""C#"""),
            _maniaTemplateLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            CreateImportStatements(),
            $@"<manialink version=""{version}"" id=""{className}"">",
            body,
            "<# RenderManiaScripts(); #>",
            "</manialink>",
            CreateTemplateParametersPreCompiled(mtComponent),
            CreateRenderAndDataMethods(),
            BuildManiaScripts(maniaScripts)
        };

        return JoinFeatureBlocks(template.ToString());
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
            snippet.AppendSnippet(
                _maniaTemplateLanguage.FeatureBlock($"public {property.Type} {propertyName} {{ get; init; }}"));
        }

        return snippet.ToString();
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

        return snippet.ToString();
    }

    /// <summary>
    /// Creates the render method for the given component node, which contains all markup/component-calls for that specific node.
    /// </summary>
    private Snippet CreateRenderMethod(MtComponentNode mtComponentNode)
    {
        var methodBody = new Snippet()
            .AppendLine(null, _maniaTemplateLanguage.FeatureBlockEnd())
            .AppendLine(mtComponentNode.TemplateContent)
            .AppendLine(null, _maniaTemplateLanguage.FeatureBlockStart());

        return CreateMethodBlock("void", GetRenderMethodName(mtComponentNode),
            DataToArguments(mtComponentNode.MtComponent),
            methodBody);
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
            var value = mtComponentNode.Attributes.Has(property.Name)
                ? mtComponentNode.Attributes.Get(property.Name)
                : GetPropertyDefaultValue(property);

            propertyAssignments.AppendLine(@$"{property.Name}: {ConvertPropertyAssignment(property, value)}");
        }

        return propertyAssignments.ToString(", ");
    }

    /// <summary>
    /// Process a template node.
    /// </summary>
    private string ProcessNode(XmlNode node, MtComponentMap availableMtComponents,
        Dictionary<int, MtComponentScript> maniaScripts, string? slotContent = null,
        bool rootContext = false)
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

                var componentNode = CreateComponentNode(
                    child,
                    component,
                    availableMtComponents,
                    attributeList,
                    slotContent,
                    maniaScripts
                );

                snippet.AppendLine(CreateComponentMethodCall(componentNode, rootContext));

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
                        snippet.AppendLine(slotContent ?? $"<!-- ERROR:{tag} -->");
                        break;
                    default:
                    {
                        var hasChildren = child.HasChildNodes;
                        snippet.AppendLine(CreateXmlOpeningTag(tag, attributeList, hasChildren));

                        if (hasChildren)
                        {
                            snippet.AppendLine(1, ProcessNode(child, availableMtComponents, maniaScripts, slotContent));
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
    private MtComponentNode CreateComponentNode(XmlNode currentNode, MtComponent mtComponent,
        MtComponentMap availableMtComponents, MtComponentAttributes attributeList, string? slotContent,
        Dictionary<int, MtComponentScript> maniaScripts)
    {
        var subComponents = availableMtComponents.Overload(mtComponent.ImportedComponents);
        var subSlotContent = ProcessNode(currentNode, subComponents, maniaScripts, slotContent);
        var componentTemplate =
            ProcessNode(XmlStringToNode(mtComponent.TemplateContent), subComponents, maniaScripts, subSlotContent);

        foreach (var script in mtComponent.Scripts)
        {
            var scriptHash = script.ContentHash();
            if (script.Once && maniaScripts.ContainsKey(scriptHash))
            {
                continue;
            }

            maniaScripts.Add(scriptHash, script);
        }

        return new MtComponentNode(
            currentNode.Name,
            mtComponent,
            attributeList,
            componentTemplate,
            Helper.UsesComponents(currentNode, availableMtComponents)
        );
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
    private string BuildManiaScripts(Dictionary<int, MtComponentScript> scripts)
    {
        var scriptMethod = new Snippet
        {
            "void RenderManiaScripts()",
            "{",
            "#>",
            "<script><!--"
        };

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
    private string CreateComponentMethodCall(MtComponentNode mtComponentNode, bool rootContext = false)
    {
        var args = ComponentNodeToMethodArguments(mtComponentNode);

        if (rootContext)
        {
            return _maniaTemplateLanguage.Code(CreateMethodCall(GetRenderMethodName(mtComponentNode), args));
        }

        return _maniaTemplateLanguage.FeatureBlock(CreateMethodCall(GetRenderMethodName(mtComponentNode), args))
            .ToString(" ");
    }

    /// <summary>
    /// Creates a method call in the target language.
    /// </summary>
    private static string CreateMethodCall(string methodName, string methodArguments = "__componentData")
    {
        return $"{methodName}({methodArguments});";
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
        return property.Default ?? $"new {property.Type}()";
    }

    /// <summary>
    /// Returns the name of the render method for a component node.
    /// </summary>
    private static string GetRenderMethodName(MtComponentNode mtComponentNode)
    {
        return $"Render{mtComponentNode.Tag}_{mtComponentNode.RenderId}";
    }

    /// <summary>
    /// Wraps a string in quotes.
    /// </summary>
    private static string WrapStringInQuotes(string str)
    {
        return $@"""{str}""";
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