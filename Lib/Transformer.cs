﻿using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Languages;

namespace ManiaTemplates.Lib;

public class Transformer
{
    private int _indentation;
    private readonly ManiaTemplateEngine _engine;
    private readonly ITargetLanguage _targetLanguage;
    private readonly Dictionary<string, Snippet> _renderMethods = new();

    public Transformer(ManiaTemplateEngine engine, ITargetLanguage targetLanguage)
    {
        _engine = engine;
        _targetLanguage = targetLanguage;
    }

    public string BuildManialink(Component component, int version = 3)
    {
        var loadedComponents = _engine.BaseComponents.Overload(component.ImportedComponents);
        var template = new Snippet
        {
            _targetLanguage.Context(@"template language=""C#"""),
            _targetLanguage.Context(@"assembly name=""C:\Users\arasz\Projects\ManiaTemplates\Tester\bin\Debug\net7.0\Models.dll"""),
            _targetLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            _targetLanguage.Context(@"import namespace=""Models"""),
            // CreateTemplateParameters(component),
            $@"<manialink version=""{version}"">",
            ProcessNode(XmlStringToNode(component.TemplateContent), loadedComponents, null, true),
            "</manialink>",
            CreateTemplateParametersPreCompiled(component),
            CreateRenderAndDataMethods()
        };

        // return template.ToString();
        return ReduceTemplateCode(template.ToString());
    }

    private string ReduceTemplateCode(string manialink)
    {
        var templateControlRegex = new Regex(@"#>\s*<#\+");
        var match = templateControlRegex.Match(manialink);
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

    private string CreateTemplateParameters(Component component)
    {
        var snippet = new Snippet();

        foreach (var (propertyName, property) in component.Properties)
        {
            snippet.AppendLine(_targetLanguage.Context(@$"parameter type=""{property.Type}"" name=""{propertyName}"""));
        }

        return snippet.ToString();
    }

    private string CreateTemplateParametersPreCompiled(Component component)
    {
        var snippet = new Snippet();

        foreach (var (propertyName, property) in component.Properties)
        {
            snippet.AppendSnippet(_targetLanguage.FeatureBlock($"public {property.Type} {propertyName} {{ get; init; }}"));
        }

        return snippet.ToString();
    }

    private string CreateRenderAndDataMethods()
    {
        Snippet snippet = new(_indentation);

        foreach (var renderMethod in _renderMethods.Values)
        {
            snippet.AppendSnippet(renderMethod);
        }

        return snippet.ToString();
    }

    private Snippet CreateRenderMethod(ComponentNode componentNode)
    {
        var methodBody = new Snippet()
            .AppendLine(null, _targetLanguage.FeatureBlockEnd())
            .AppendLine(componentNode.TemplateContent)
            .AppendLine(null, _targetLanguage.FeatureBlockStart());

        return CreateMethodBlock("void", GetRenderMethodName(componentNode), DataToArguments(componentNode.Component),
            methodBody);
    }

    private Snippet CreateMethodBlock(string returnType, string methodName, string arguments, Snippet body)
    {
        var methodBlock = new Snippet(1)
            .AppendLine($"{returnType} {methodName}({arguments})")
            .AppendLine("{")
            .AppendSnippet(body)
            .AppendLine("}")
            .ToString();

        return _targetLanguage.FeatureBlock(methodBlock);
    }

    private string ComponentNodeToMethodArguments(ComponentNode componentNode)
    {
        Snippet propertyAssignments = new();

        foreach (var property in componentNode.Component.Properties.Values)
        {
            var value = componentNode.Attributes.Has(property.Name)
                ? componentNode.Attributes.Get(property.Name)
                : GetPropertyDefaultValue(property);

            var assignment = ConvertPropertyAssignment(property, value);

            propertyAssignments.AppendLine(@$"{property.Name}: {assignment}");
        }

        return propertyAssignments.ToString(", ");
    }

    private string ProcessNode(XmlNode node, ComponentList availableComponents, string? slotContent = null,
        bool rootContext = false)
    {
        Snippet snippet = new(_indentation + 1);

        foreach (XmlNode child in node.ChildNodes)
        {
            var tag = child.Name;
            var attributeList = GetNodeAttributes(child);

            string? forEachLoop = null;
            if (attributeList.Has("foreach"))
            {
                forEachLoop = attributeList.Get("foreach");
                snippet.AppendLine(null, _targetLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " int __index = 0;");
                snippet.AppendLine(null, $" foreach({forEachLoop})");
                snippet.AppendLine(null, " {");
                snippet.AppendLine(null, _targetLanguage.FeatureBlockEnd());
            }

            if (availableComponents.ContainsKey(tag))
            {
                var componentNode = CreateComponentNode(
                    child,
                    availableComponents[tag],
                    availableComponents,
                    attributeList,
                    slotContent
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
                            snippet.AppendLine(1, ProcessNode(child, availableComponents, slotContent));
                            snippet.AppendLine(CreateXmlClosingTag(tag));
                        }

                        break;
                    }
                }
            }

            if (forEachLoop != null)
            {
                snippet.AppendLine(null, _targetLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " __index++;");
                snippet.AppendLine(null, " }");
                snippet.AppendLine(null, _targetLanguage.FeatureBlockEnd());
            }
        }

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

    private string DataToArguments(Component component)
    {
        var snippet = new Snippet();

        foreach (var property in component.Properties.Values)
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
            // var newBody = ConvertCurlyContent(body);
            var newBody = body;

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

        output = Regex.Replace(output, @"(^("""")?\s*[+\-*/%]\s*|\s*[+\-*/%]\s*("""")?$)", ""); //Hotfix

        return output;
    }

    private string CreateComponentMethodCall(ComponentNode componentNode, bool rootContext = false)
    {
        var args = ComponentNodeToMethodArguments(componentNode);

        if (rootContext)
        {
            return _targetLanguage.Code(CreateMethodCall(GetRenderMethodName(componentNode), args));
        }

        return _targetLanguage.FeatureBlock(CreateMethodCall(GetRenderMethodName(componentNode), args))
            .ToString(" ");
    }

    private string CreateMethodCall(string methodName, string methodArguments = "__componentData",
        string lineSuffix = ";")
    {
        return $"{methodName}({methodArguments})" + lineSuffix;
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
        return $"Render{componentNode.Tag}_{componentNode.RenderId}";
    }

    private static string Quotes(string str)
    {
        return $@"""{str}""";
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