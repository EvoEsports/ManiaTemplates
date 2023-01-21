using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Languages;

namespace ManiaTemplates.Lib;

public class Transformer
{
    private const string BootstrapMethodName = "RenderManialink";

    private int _indentation;
    private readonly ManiaTemplateEngine _engine;
    private readonly ITargetLanguage _targetLanguage;
    private readonly List<string> _renderMethods;
    private readonly List<string> _dataMethods;

    public Transformer(ManiaTemplateEngine engine, ITargetLanguage targetLanguage)
    {
        _engine = engine;
        _targetLanguage = targetLanguage;
        _renderMethods = new List<string>();
        _dataMethods = new List<string>();
    }

    public string BuildManialink(Component component)
    {
        var loadedComponents = _engine.BaseComponents.Overload(component.ImportedComponents);

        var template = new List<string>
        {
            _targetLanguage.Context(@"template language=""C#"""),
            _targetLanguage.Context(@"assembly name=""Microsoft.CSharp"""),
            // T4Directive(@"output extension="".xml"""),
            // T4Directive(@"import namespace=""System.Collections.Generic"""),
            CreateTemplateParameters(component),
            @"<manialink version=""3"">",
            CreateMethodCall(BootstrapMethodName, PropertiesToAnonymousType(component)),
            @"</manialink>",
            CreateRenderManialinkMethod(component, loadedComponents),
            CreateRenderAndDataMethods()
        };

        var manialink = string.Join("\n", template);
        var pattern = new Regex(@"#>\s*<#\+");
        var match = pattern.Match(manialink);

        while (match.Success)
        {
            manialink = manialink.Replace(match.ToString(), "\n");

            match = match.NextMatch();
        }

        return manialink;
    }

    private string CreateTemplateParameters(Component component)
    {
        var snippet = new Snippet();

        foreach (var (propertyName, property) in component.Properties)
        {
            snippet.AppendLine(_targetLanguage.Context(@$"parameter type=""{property.Type}"" name=""{propertyName}"""));
        }

        return snippet.ToString();
    }

    private string CreateRenderAndDataMethods()
    {
        Snippet snippet = new(_indentation);
        var addedDataMethods = new List<string>();

        foreach (var renderMethod in _renderMethods)
        {
            snippet.AppendLine(renderMethod);
        }

        foreach (var dataMethod in _dataMethods)
        {
            var hash = Helper.Hash(dataMethod);
            if (addedDataMethods.Contains(hash)) continue;
            snippet.AppendLine(dataMethod);
            addedDataMethods.Add(hash);
        }

        return snippet.ToString();
    }

    private string CreateDataMethod(ComponentNode componentNode)
    {
        var methodBody = new Snippet()
            .AppendLine("Type __type = __data.GetType();")
            .AppendLine("return new {")
            .AppendSnippet(1, ComponentNodeToPropertyAssignments(componentNode))
            .AppendLine("};");

        return CreateMethodBlock("dynamic", GetDataMethodName(componentNode), "dynamic __data", methodBody);
    }

    private string CreateRenderMethod(ComponentNode componentNode)
    {
        var methodBody = new Snippet()
            .AppendLine(1, NewDataContext(componentNode))
            .AppendLine(null, _targetLanguage.FeatureBlockEnd())
            .AppendLine(componentNode.TemplateContent)
            .AppendLine(null, _targetLanguage.FeatureBlockStart());

        return CreateMethodBlock("void", GetRenderMethodName(componentNode), "dynamic __data", methodBody);
    }

    private string CreateRenderManialinkMethod(Component component, ComponentList loadedComponents)
    {
        var body = new Snippet()
            .AppendLine(_targetLanguage.FeatureBlockEnd())
            .AppendLine(ProcessNode(XmlStringToNode(component.TemplateContent), loadedComponents))
            .AppendLine(_targetLanguage.FeatureBlockStart());

        return CreateMethodBlock("void", BootstrapMethodName, "dynamic __componentData", body);
    }

    private string CreateMethodBlock(string returnType, string methodName, string arguments, Snippet body)
    {
        var methodBlock = new Snippet()
            .AppendLine($"{returnType} {methodName}({arguments}){{")
            .AppendSnippet(1, body)
            .AppendLine("}")
            .ToString();

        return _targetLanguage.FeatureBlock(methodBlock);
    }

    private Snippet ComponentNodeToPropertyAssignments(ComponentNode componentNode)
    {
        Snippet propertyAssignments = new();

        foreach (var property in componentNode.Component.Properties.Values)
        {
            var value = componentNode.Attributes.Has(property.Name)
                ? componentNode.Attributes.Get(property.Name)
                : GetPropertyDefaultValue(property);

            var assignment = ConvertPropertyAssignment(property, value);

            Console.WriteLine(assignment);

            propertyAssignments.AppendLine(@$"{property.Name} = {assignment}");
        }

        return propertyAssignments;
    }

    private string ProcessNode(XmlNode node, ComponentList availableComponents, string? slotContent = null)
    {
        Snippet snippet = new(_indentation + 1);
        var oldIndentation = _indentation;
        _indentation = 0;

        foreach (XmlNode child in node.ChildNodes)
        {
            var tag = child.Name;
            var attributeList = GetNodeAttributes(child);

            if (availableComponents.ContainsKey(tag))
            {
                var componentNode = CreateComponentNode(
                    child,
                    availableComponents[tag],
                    availableComponents,
                    attributeList,
                    slotContent
                );

                snippet.AppendLine(-1, CreateComponentMethodCall(componentNode));
                _renderMethods.Add(CreateRenderMethod(componentNode));
                _dataMethods.Add(CreateDataMethod(componentNode));
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
                            snippet.AppendLine(1, ProcessNode(child, availableComponents, slotContent));
                            snippet.AppendLine(CreateXmlClosingTag(tag));
                        }

                        break;
                    }
                }
            }
        }

        //Restore indentation
        _indentation = oldIndentation;

        return snippet.ToString();
    }

    private static ComponentAttributes GetNodeAttributes(XmlNode node)
    {
        var attributeList = new ComponentAttributes();
        if (node.Attributes == null) return attributeList;

        foreach (XmlAttribute attribute in node.Attributes)
        {
            attributeList.Add(attribute.Name, attribute.Value);
        }

        return attributeList;
    }

    private ComponentNode CreateComponentNode(XmlNode currentNode, Component component,
        ComponentList availableComponents, ComponentAttributes attributeList, string? slotContent)
    {
        var subComponents = availableComponents.Overload(component.ImportedComponents);
        var subSlotContent = ProcessNode(currentNode, subComponents, slotContent);
        var componentTemplate = ProcessNode(XmlStringToNode(component.TemplateContent), subComponents, subSlotContent);

        return new ComponentNode(
            currentNode.Name,
            component,
            attributeList,
            componentTemplate,
            Helper.UsesComponents(currentNode, availableComponents)
        );
    }

    private string NewDataContext(ComponentNode componentNode)
    {
        var component = componentNode.Component;

        Snippet snippet = new(_indentation + 2);
        snippet.AppendLine("Type __type = __data.GetType();");

        foreach (var property in component.Properties.Values)
        {
            var defaultValue = WrapIfString(property, GetPropertyDefaultValue(property));
            snippet.AppendLine(
                @$"{property.Type} {property.Name} = __type.GetProperty(""{property.Name}"")?.GetValue(__data) ?? {defaultValue};");
        }

        snippet.AppendLine(CreateComponentDataVariable(component));

        return snippet.ToString();
    }

    private static string ConvertPropertyAssignment(ComponentProperty property, string parameterValue)
    {
        var curlyMatcher = new Regex(@"\{\{\s*(.+?)\s*\}\}");
        var output = WrapIfString(property, parameterValue);
        var matches = curlyMatcher.Match(output);
        var isStringType = IsStringType(property);
        var offset = 0;

        while (matches.Success)
        {
            var group0 = matches.Groups[0];
            var body = matches.Groups[1].Value;

            var matchStart = group0.Index - offset;
            var matchLength = group0.Length - offset;
            var newBody = ConvertCurlyContent(body);

            if (isStringType)
            {
                newBody = $" + {newBody} + ";
            }

            if (matchLength == parameterValue.Length)
            {
                output = newBody;
                break;
            }

            //Update output string
            output = output[..matchStart] + Quotes(newBody) + output[(matchStart + matchLength)..];

            //Update offset for next match and proceed
            offset += matchLength - newBody.Length;

            //Look for next match
            matches = curlyMatcher.Match(output);
        }

        output = Regex.Replace(output, @"(^("""")?\s*\+\s*|\s*\+\s*("""")?$)", ""); //Hotfix

        return output + ",";
    }

    private static string ConvertCurlyContent(string curlyContent)
    {
        var variableMatcher = new Regex(@"([a-z][a-zA-Z]*)([?!]?(?:\.\w+|\[.+?\]))?");
        var output = curlyContent;
        var match = variableMatcher.Match(output);
        var offset = 0;

        while (match.Success)
        {
            var group0 = match.Groups[0];
            var body = match.Groups[1].Value;
            var mutator = match.Groups[2].Value;

            var newBody = $"__type.GetProperty({Quotes(body)})?.GetValue(__data){mutator}";

            var matchStart = group0.Index + offset;
            var matchLength = group0.Length;

            output = output[..matchStart]
                     + newBody
                     + output.Substring(matchStart + matchLength);

            offset += newBody.Length - matchLength;

            match = match.NextMatch();
        }

        return output;
    }

    private string CreateComponentMethodCall(ComponentNode componentNode)
    {
        return CreateMethodCall(GetDataMethodName(componentNode), "__componentData", false);
    }

    private string CreateMethodCall(string methodName, string methodArguments = "__componentData",
        bool featureBlock = true)
    {
        if (featureBlock)
        {
            return _targetLanguage.Code($"{methodName}({methodArguments});");
        }

        return _targetLanguage.CallMethod($"{methodName}({methodArguments});");
    }

    private static string WrapIfString(ComponentProperty property, string value)
    {
        return IsStringType(property) ? @$"""{value}""" : value;
    }

    private static bool IsStringType(ComponentProperty property)
    {
        return property.Type.ToLower().Contains("string"); //TODO: find better way to determine string
    }

    private static string GetPropertyDefaultValue(ComponentProperty property)
    {
        return property.Default ?? $"new {property.Type}()";
    }

    private static string GetRenderMethodName(ComponentNode componentNode)
    {
        if (componentNode.HasSlot || true)
        {
            return $"Render{componentNode.Tag}_{componentNode.Id}";
        }

        //TODO: slot methods
        return $"Render{componentNode.Tag}";
    }

    private static string GetDataMethodName(ComponentNode componentNode)
    {
        return $"Data{componentNode.Tag}_{componentNode.Id}";
    }

    private static string Quotes(string str)
    {
        return $@"""{str}""";
    }

    private static string CreateComponentDataVariable(Component component)
    {
        return "var __componentData = " + PropertiesToAnonymousType(component) + ";";
    }

    private static string PropertiesToAnonymousType(Component component)
    {
        return "new { " + string.Join(", ", component.Properties.Keys) + " }";
    }

    private string CreateXmlOpeningTag(string tag, ComponentAttributes attributeList, bool hasChildren)
    {
        var output = $"<{tag}";

        foreach (var (attributeName, attributeValue) in attributeList.All())
        {
            output += @$" {attributeName}=""{ReplaceCurlyBraces(attributeValue)}""";
        }

        if (!hasChildren)
        {
            output += " /";
        }

        return output + ">";
    }

    private static string CreateXmlClosingTag(string tag)
    {
        return $"</{tag}>";
    }

    private XmlNode XmlStringToNode(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        return doc.FirstChild!;
    }

    private string ReplaceCurlyBraces(string value)
    {
        var curlyMatcher = new Regex(@"\{\{(.+?)\}\}");
        var matches = curlyMatcher.Match(value);
        var output = value;

        while (matches.Success)
        {
            var match = matches.Groups[0].Value.Trim();
            var content = matches.Groups[1].Value.Trim();

            output = output.Replace(match, _targetLanguage.InsertResult(content));

            matches = matches.NextMatch();
        }

        return output;
    }
}