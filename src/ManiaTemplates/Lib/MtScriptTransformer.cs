using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

public class MtScriptTransformer(IManiaTemplateLanguage templateLanguage) : ICurlyBraceMethods
{
    private readonly Dictionary<string, string> _maniaScriptIncludes = new();
    private readonly Dictionary<string, string> _maniaScriptConstants = new();
    private readonly Dictionary<string, string> _maniaScriptStructs = new();

    private static readonly Regex ManiaScriptIncludeRegex = new(@"#Include\s+""(.+?)""\s+as\s+([_a-zA-Z]+)");
    private static readonly Regex ManiaScriptConstantRegex = new(@"#Const\s+([a-zA-Z_:]+)\s+.+");
    private static readonly Regex ManiaScriptStructRegex = new(@"(?s)#Struct\s+([_a-zA-Z]+)\s*\{.+?\}");

    public string CreateManiaScriptBlock(MtComponent component)
    {
        var renderMethod = new StringBuilder("<script>");

        foreach (var script in component.Scripts)
        {
            if (script.Once)
            {
                renderMethod.AppendLine(templateLanguage.FeatureBlockStart())
                    .AppendLine($@"if(!__insertedOneTimeManiaScripts.Contains(""{script.ContentHash()}"")){{")
                    .AppendLine(templateLanguage.FeatureBlockEnd());
            }

            renderMethod.AppendLine(ICurlyBraceMethods.ReplaceCurlyBraces(ExtractManiaScriptDirectives(script.Content),
                templateLanguage.InsertResult));

            if (script.Once)
            {
                renderMethod.AppendLine(templateLanguage.FeatureBlockStart())
                    .AppendLine($@"__insertedOneTimeManiaScripts.Add(""{script.ContentHash()}"");")
                    .AppendLine("}")
                    .AppendLine(templateLanguage.FeatureBlockEnd());
            }
        }

        return renderMethod.AppendLine("</script>").ToString();
    }

    /// <summary>
    /// Inserts a block with ManiaScript, that contains directives like includes, constants and custom structs.
    /// </summary>
    public string CreateManiaScriptDirectivesBlock()
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
    /// Extracts directives like structs, includes or constants from a given ManiaScript block.
    /// </summary>
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
}