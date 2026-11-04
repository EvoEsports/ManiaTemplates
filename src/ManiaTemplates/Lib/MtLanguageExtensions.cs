using System.Text;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

/// <summary>
/// Code generation shared by all template languages.
/// </summary>
internal static class MtLanguageExtensions
{
    /// <summary>
    /// Creates a method to render template contents.
    /// </summary>
    /// <param name="language">The template language.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="arguments">The parameters of the method.</param>
    /// <param name="body">The template contents rendered by the method.</param>
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
