using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

public class MtScriptTransformer(IManiaTemplateLanguage templateLanguage)
{
    private readonly Dictionary<string, string> _maniaScriptIncludes = new();
    private readonly Dictionary<string, string> _maniaScriptConstants = new();
    private readonly Dictionary<string, string> _maniaScriptStructs = new();
    private string? _mainScriptHash;

    private static readonly Regex ManiaScriptIncludeRegex = new(@"#Include\s+""(.+?)""\s+as\s+([_a-zA-Z]+)");
    private static readonly Regex ManiaScriptConstantRegex = new(@"#Const\s+([a-zA-Z_:]+)\s+.+");
    private static readonly Regex ManiaScriptStructRegex = new(@"(?s)#Struct\s+([_a-zA-Z]+)\s*\{.+?\}");
    private static readonly Regex WhitespaceRegex = new(@"\s+");

    public string CreateManiaScriptBlock(MtComponent component)
    {
        var renderMethod = new StringBuilder("<script><!--").AppendLine();

        foreach (var script in component.Scripts)
        {
            EnsureSingleMainMethod(script);

            var scriptHash = script.ContentHash();
            if (script.Once)
            {
                renderMethod.AppendLine(templateLanguage.FeatureBlockStart())
                    .AppendLine($@"if(!__insertedOneTimeManiaScripts.Contains(""{scriptHash}"")){{")
                    .AppendLine(templateLanguage.FeatureBlockEnd());
            }

            renderMethod.AppendLine(
                CurlyBraceUtils.ReplaceCurlyBracesWithRawOutput(
                    ExtractManiaScriptDirectives(script.Content),
                    content => templateLanguage.InsertResult($"MtEscaper.EscapeManiaScript({content})"),
                    content => templateLanguage.InsertResult($"MtEscaper.EscapeRawManiaScript({content})")
                )
            );

            if (script.Once)
            {
                renderMethod.AppendLine(templateLanguage.FeatureBlockStart())
                    .AppendLine($@"__insertedOneTimeManiaScripts.Add(""{scriptHash}"");")
                    .AppendLine("}")
                    .AppendLine(templateLanguage.FeatureBlockEnd());
            }
        }

        return renderMethod.AppendLine("--></script>").ToString();
    }

    /// <summary>
    /// Ensures that only one distinct script in the ManiaLink declares a main method.
    /// </summary>
    private void EnsureSingleMainMethod(MtComponentScript script)
    {
        if (!script.HasMainMethod)
        {
            return;
        }

        var scriptHash = script.ContentHash();
        if (_mainScriptHash != null && _mainScriptHash != scriptHash)
        {
            throw new DuplicateMainManiaScriptException(
                $"Found multiple scripts with a main method, only one is allowed per ManiaLink: {script.Content}");
        }

        _mainScriptHash = scriptHash;
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
        // Each directive is removed from the script after registering it
        var output = ManiaScriptIncludeRegex.Replace(maniaScriptSource, match =>
        {
            var libraryToInclude = match.Groups[1].Value;
            var includedAs = match.Groups[2].Value;

            if (!_maniaScriptIncludes.TryAdd(includedAs, libraryToInclude) &&
                _maniaScriptIncludes[includedAs] != libraryToInclude)
            {
                throw new DuplicateManiaScriptIncludeException(
                    $"Can't include {libraryToInclude} as {includedAs}, because another include ({_maniaScriptIncludes[includedAs]}) blocks it.");
            }

            return "";
        });

        output = ManiaScriptConstantRegex.Replace(output, match =>
        {
            var constantName = match.Groups[1].Value;
            var constantDefinition = match.Value.Trim();

            if (!_maniaScriptConstants.TryAdd(constantName, constantDefinition) &&
                WhitespaceRegex.Replace(constantDefinition, " ") !=
                WhitespaceRegex.Replace(_maniaScriptConstants[constantName], " "))
            {
                throw new DuplicateManiaScriptConstantException(
                    $"Can't redefine constant {constantName}, because it is already defined as: {_maniaScriptConstants[constantName]}.");
            }

            return "";
        });

        return ManiaScriptStructRegex.Replace(output, match =>
        {
            var structName = match.Groups[1].Value;
            var structDefinition = match.Value.Trim();

            if (!_maniaScriptStructs.TryAdd(structName, structDefinition) &&
                string.Compare(structDefinition, _maniaScriptStructs[structName], CultureInfo.CurrentCulture,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols) != 0)
            {
                throw new DuplicateManiaScriptStructException(
                    $"Can't redefine struct {structName}, because it is already defined as: {_maniaScriptStructs[structName]}.");
            }

            return "";
        });
    }
}