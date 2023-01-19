namespace ManiaTemplates.Languages;

public class T4 : ITargetLanguage
{
    public string Context(string content)
    {
        return $"<#@ {content} #>";
    }

    public string InsertResult(string content)
    {
        return $"<#= {content} #>";
    }

    public string Code(string content)
    {
        return $"<# {content} #>";
    }

    public string FeatureBlock(string content)
    {
        return $"<#+ {content} #>";
    }

    public string FeatureBlockStart()
    {
        return "<#+";
    }

    public string FeatureBlockEnd()
    {
        return "#>";
    }

    public string CallMethod(string methodExpression)
    {
        return FeatureBlock(methodExpression);
    }
}