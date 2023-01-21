using ManiaTemplates.Lib;

namespace ManiaTemplates.Languages;

public interface ITargetLanguage
{
    public string Context(string content);
    public string InsertResult(string content);
    public string Code(string content);
    public Snippet FeatureBlock(string content);
    public string FeatureBlockStart();
    public string FeatureBlockEnd();
    public string CallMethod(string methodExpression);
}