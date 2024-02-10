using System.Text.RegularExpressions;
using ManiaTemplates.Interfaces;
using ManiaTemplates.Lib;

namespace ManiaTemplates.Languages;

public class MtLanguageT4 : IManiaTemplateLanguage
{
    private static readonly Regex TemplateFeatureControlRegex = new(@"#>\s*<#\+");
    
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

    public string OptimizeOutput(string generatedContent)
    {
        return JoinFeatureBlocks(generatedContent);
    }

    /// <summary>
    /// Joins consecutive feature blocks to reduce generated code.
    /// </summary>
    private static string JoinFeatureBlocks(string manialink)
    {
        var match = TemplateFeatureControlRegex.Match(manialink);
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
}