using ManiaTemplates.Interfaces;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Languages;

public class MtLanguageT4 : IManiaTemplateLanguage
{
    public string Context(string content)
    {
        return $"<#@ {content} #>";
    }

    public string InsertResult(string content)
    {
        return $"<#= ({content}) #>";
    }

    public string Code(string content)
    {
        return $"<# {content} #>";
    }

    public Snippet FeatureBlock(string content)
    {
        return new Snippet()
            .AppendLine(FeatureBlockStart())
            .AppendLine(content)
            .AppendLine(FeatureBlockEnd());
    }

    public string FeatureBlockStart()
    {
        return "<#+";
    }

    public string FeatureBlockEnd()
    {
        return "#>";
    }
}