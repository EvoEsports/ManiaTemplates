using System.Text;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

internal static class MtLanguageExtensions
{
    /// <summary>
    /// Creates a method to render template contents.
    /// </summary>
    public static string RenderMethod(this IManiaTemplateLanguage language, string name,
        IEnumerable<string> arguments, string body)
    {
        return new StringBuilder(language.FeatureBlockStart())
            .Append($"private void {name}(")
            .Append(string.Join(", ", arguments))
            .AppendLine(") {")
            .AppendLine(language.FeatureBlockEnd())
            .AppendLine(body)
            .AppendLine(language.FeatureBlockStart())
            .Append('}')
            .AppendLine(language.FeatureBlockEnd())
            .ToString();
    }
}
