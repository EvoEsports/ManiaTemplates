using ManiaTemplates.Lib;

namespace ManiaTemplates.Interfaces;

public interface IManiaTemplateLanguage
{
    public string Context(string content);
    public string InsertResult(string content);
    public string Code(string content);
    public Snippet FeatureBlock(string content);
    public string FeatureBlockStart();
    public string FeatureBlockEnd();
    public string OptimizeOutput(string generatedContent) => generatedContent;
}