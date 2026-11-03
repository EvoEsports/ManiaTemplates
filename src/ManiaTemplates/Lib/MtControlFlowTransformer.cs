using System.Xml;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

/// <summary>
/// Creates the code of conditions and loops placed on nodes.
/// </summary>
internal class MtControlFlowTransformer(IManiaTemplateLanguage maniaTemplateLanguage)
{
    private int _conditionChainCount;

    /// <summary>
    /// Throws if a node has a condition and a loop, as it is ambiguous whether the condition applies to every
    /// iteration or the whole loop.
    /// </summary>
    public static void EnsureLoopAndConditionAreNotCombined(string tag, MtLoop? loop, MtCondition? condition)
    {
        if (loop != null && condition != null)
        {
            throw new InvalidConditionException(
                $"The {condition.AttributeName} attribute on <{tag}> must not be combined with a {loop.Keyword} loop, put them on separate nodes instead.");
        }
    }

    /// <summary>
    /// Returns the name of the variable tracking whether a condition of the chain the node belongs to was met.
    /// Returns null for nodes without condition and if-conditions not continued by an else-if or else.
    /// </summary>
    public string? GetConditionChainVariable(XmlNode node, MtCondition? condition,
        string? openConditionChainVariable)
    {
        switch (condition?.Type)
        {
            case null:
                return null;

            case MtConditionType.If:
                return IsConditionChainContinued(node) ? $"__conditionChain{++_conditionChainCount}" : null;

            default:
                return openConditionChainVariable ?? throw new InvalidConditionException(
                    $"The {condition.AttributeName} attribute on <{node.Name}> must follow a node with an if or else-if attribute, only comments may be in between.");
        }
    }

    /// <summary>
    /// Checks whether the next sibling node, ignoring comments, continues a condition chain with else-if or else.
    /// </summary>
    private static bool IsConditionChainContinued(XmlNode node)
    {
        var sibling = node.NextSibling;
        while (sibling is XmlComment)
        {
            sibling = sibling.NextSibling;
        }

        return sibling?.Attributes?["else-if"] != null || sibling?.Attributes?["else"] != null;
    }

    /// <summary>
    /// Wraps the snippet in a condition block.
    /// Conditions of a chain are rendered as separate if-blocks sharing a variable that tracks whether a condition
    /// was met, which allows rendering comments between them.
    /// </summary>
    public MtCodeSnippet CreateConditionBlock(MtCodeSnippet input, MtCondition condition, string? conditionChainVariable)
    {
        var snippet = new MtCodeSnippet();
        var expression = CurlyBraceUtils.RemoveCurlyBraces(condition.Expression);

        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart());

        if (conditionChainVariable == null)
        {
            snippet.AppendLine($"if ({expression}) {{");
        }
        else
        {
            switch (condition.Type)
            {
                case MtConditionType.If:
                    snippet.AppendLine($"var {conditionChainVariable} = false;");
                    snippet.AppendLine($"if ({expression}) {{");
                    break;
                case MtConditionType.ElseIf:
                    snippet.AppendLine($"if (!{conditionChainVariable} && ({expression})) {{");
                    break;
                default:
                    snippet.AppendLine($"if (!{conditionChainVariable}) {{");
                    break;
            }

            if (condition.Type != MtConditionType.Else)
            {
                snippet.AppendLine($"{conditionChainVariable} = true;");
            }
        }

        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine("}");
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }

    /// <summary>
    /// Wraps the snippet in a loop, exposing the zero-based iteration index to the loop body.
    /// The surrounding block scopes the iteration counter, so sibling loops do not collide.
    /// </summary>
    public MtCodeSnippet CreateLoopBlock(MtCodeSnippet input, MtLoop loop)
    {
        var snippet = new MtCodeSnippet();
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine("{");
        snippet.AppendLine($"var {loop.CounterVariableName} = 0;");
        snippet.AppendLine($"{loop.Keyword} ({loop.Condition}) {{");
        snippet.AppendLine($"var {loop.IndexVariableName} = {loop.CounterVariableName}++;");
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockEnd());
        snippet.AppendSnippet(input);
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockStart());
        snippet.AppendLine("}");
        snippet.AppendLine("}");
        snippet.AppendLine(maniaTemplateLanguage.FeatureBlockEnd());

        return snippet;
    }
}
