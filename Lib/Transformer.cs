﻿using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;

namespace ManiaTemplates.Lib;

public class Transformer
{
    private readonly ManiaTemplateEngine _engine;
    private readonly IMtLanguage _mtLanguage;
    private readonly Dictionary<string, Snippet> _renderMethods = new();
    private readonly List<string> _namespaces = new();
    
    private static readonly Regex TemplateControlRegex = new(@"#>\s*<#\+");
    private static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");
    private static readonly Regex TemplateReplacerRegex = new(@"(^("""")?\s*[+\-*/%]\s*|\s*[+\-*/%]\s*("""")?$)");

    public Transformer(ManiaTemplateEngine engine, IMtLanguage mtLanguage)
    {
        _engine = engine;
        _mtLanguage = mtLanguage;
    }

    public string BuildManialink(MtComponent mtComponent, int version = 3)
    {
        var loadedComponents = _engine.BaseMtComponents.Overload(mtComponent.ImportedMtComponents);
        var body = ProcessNode(XmlStringToNode(mtComponent.TemplateContent), loadedComponents, null, true);
        var template = new Snippet
        {
            _mtLanguage.Context(@"template language=""C#"""),
            _mtLanguage.Context(@"import namespace=""System.Collections.Generic"""),
            CreateImportStatements(),
            $@"<manialink version=""{version}"">",
            body,
            "</manialink>",
            CreateTemplateParametersPreCompiled(mtComponent),
            CreateRenderAndDataMethods()
        };

        return JoinFeatureBlocks(template.ToString());
    }

    private string CreateImportStatements()
    {
        var snippet = new Snippet();

        foreach (var ns in _namespaces)
        {
            snippet.AppendLine(_mtLanguage.Context($@"import namespace=""{ns}"""));
        }

        return snippet.ToString();
    }

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

    private string CreateTemplateParametersPreCompiled(MtComponent mtComponent)
    {
        var snippet = new Snippet();

        foreach (var (propertyName, property) in mtComponent.Properties)
        {
            snippet.AppendSnippet(
                _mtLanguage.FeatureBlock($"public {property.Type} {propertyName} {{ get; init; }}"));
        }

        return snippet.ToString();
    }

    private string CreateRenderAndDataMethods()
    {
        Snippet snippet = new();

        foreach (var renderMethod in _renderMethods.Values)
        {
            snippet.AppendSnippet(renderMethod);
        }

        return snippet.ToString();
    }

    private Snippet CreateRenderMethod(MtComponentNode mtComponentNode)
    {
        var methodBody = new Snippet()
            .AppendLine(null, _mtLanguage.FeatureBlockEnd())
            .AppendLine(mtComponentNode.TemplateContent)
            .AppendLine(null, _mtLanguage.FeatureBlockStart());

        return CreateMethodBlock("void", GetRenderMethodName(mtComponentNode), DataToArguments(mtComponentNode.MtComponent),
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

        return _mtLanguage.FeatureBlock(methodBlock);
    }

    private string ComponentNodeToMethodArguments(MtComponentNode mtComponentNode)
    {
        Snippet propertyAssignments = new();

        foreach (var property in mtComponentNode.MtComponent.Properties.Values)
        {
            var value = mtComponentNode.Attributes.Has(property.Name)
                ? mtComponentNode.Attributes.Get(property.Name)
                : GetPropertyDefaultValue(property);

            var assignment = ConvertPropertyAssignment(property, value);

            propertyAssignments.AppendLine(@$"{property.Name}: {assignment}");
        }

        return propertyAssignments.ToString(", ");
    }

    private string ProcessNode(XmlNode node, MtComponentList availableMtComponents, string? slotContent = null,
        bool rootContext = false)
    {
        Snippet snippet = new();

        foreach (XmlNode child in node.ChildNodes)
        {
            var tag = child.Name;
            var attributeList = GetNodeAttributes(child);

            string? forEachLoop = null;
            if (attributeList.Has("foreach"))
            {
                forEachLoop = attributeList.Pull("foreach");
                snippet.AppendLine(null, _mtLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " int __index = 0;");
                snippet.AppendLine(null, $" foreach({forEachLoop})");
                snippet.AppendLine(null, " {");
                snippet.AppendLine(null, _mtLanguage.FeatureBlockEnd());
            }

            if (availableMtComponents.ContainsKey(tag))
            {
                foreach (var ns in availableMtComponents[tag].Namespaces)
                {
                    _namespaces.Add(ns);
                }

                var componentNode = CreateComponentNode(
                    child,
                    availableMtComponents[tag],
                    availableMtComponents,
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
                            snippet.AppendLine(1, ProcessNode(child, availableMtComponents, slotContent));
                            snippet.AppendLine(CreateXmlClosingTag(tag));
                        }

                        break;
                    }
                }
            }

            if (forEachLoop != null)
            {
                snippet.AppendLine(null, _mtLanguage.FeatureBlockStart());
                snippet.AppendLine(null, " __index++;");
                snippet.AppendLine(null, " }");
                snippet.AppendLine(null, _mtLanguage.FeatureBlockEnd());
            }
        }

        return snippet.ToString();
    }

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

    private MtComponentNode CreateComponentNode(XmlNode currentNode, MtComponent mtComponent,
        MtComponentList availableMtComponents, MtComponentAttributes attributeList, string? slotContent)
    {
        var subComponents = availableMtComponents.Overload(mtComponent.ImportedMtComponents);
        var subSlotContent = ProcessNode(currentNode, subComponents, slotContent);
        var componentTemplate = ProcessNode(XmlStringToNode(mtComponent.TemplateContent), subComponents, subSlotContent);

        return new MtComponentNode(
            currentNode.Name,
            mtComponent,
            attributeList,
            componentTemplate,
            Helper.UsesComponents(currentNode, availableMtComponents)
        );
    }

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

    private static string ConvertPropertyAssignment(MtComponentProperty property, string parameterValue)
    {
        var output = WrapIfString(property, parameterValue);
        var matches = TemplateInterpolationRegex.Match(output);
        var isStringType = IsStringType(property);
        var offset = 0;

        while (matches.Success)
        {
            var group0 = matches.Groups[0];
            var body = matches.Groups[1].Value;

            var matchStart = group0.Index - offset;
            var matchLength = group0.Length - offset;
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
            matches = TemplateInterpolationRegex.Match(output);
        }

        output = TemplateReplacerRegex.Replace(output, ""); //Hotfix

        return output;
    }

    private string CreateComponentMethodCall(MtComponentNode mtComponentNode, bool rootContext = false)
    {
        var args = ComponentNodeToMethodArguments(mtComponentNode);

        if (rootContext)
        {
            return _mtLanguage.Code(CreateMethodCall(GetRenderMethodName(mtComponentNode), args));
        }

        return _mtLanguage.FeatureBlock(CreateMethodCall(GetRenderMethodName(mtComponentNode), args))
            .ToString(" ");
    }

    private string CreateMethodCall(string methodName, string methodArguments = "__componentData",
        string lineSuffix = ";")
    {
        return $"{methodName}({methodArguments})" + lineSuffix;
    }

    private static string WrapIfString(MtComponentProperty property, string value)
    {
        return IsStringType(property) ? @$"""{value}""" : value;
    }

    private static bool IsStringType(MtComponentProperty property)
    {
        return property.Type.ToLower().Contains("string"); //TODO: find better way to determine string
    }

    private static string GetPropertyDefaultValue(MtComponentProperty property)
    {
        return property.Default ?? $"new {property.Type}()";
    }

    private static string GetRenderMethodName(MtComponentNode mtComponentNode)
    {
        return $"Render{mtComponentNode.Tag}_{mtComponentNode.RenderId}";
    }

    private static string Quotes(string str)
    {
        return $@"""{str}""";
    }

    private string CreateXmlOpeningTag(string tag, MtComponentAttributes attributeList, bool hasChildren)
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
        var matches = TemplateInterpolationRegex.Match(value);
        var output = value;

        while (matches.Success)
        {
            var match = matches.Groups[0].Value.Trim();
            var content = matches.Groups[1].Value.Trim();

            output = output.Replace(match, _mtLanguage.InsertResult(content));

            matches = matches.NextMatch();
        }

        return output;
    }
}