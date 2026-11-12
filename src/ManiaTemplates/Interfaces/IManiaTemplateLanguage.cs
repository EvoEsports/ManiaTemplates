using ManiaTemplates.Lib;

namespace ManiaTemplates.Interfaces;

/// <summary>
/// The template language the transformer generates, which is pre-processed into C# code.
/// </summary>
public interface IManiaTemplateLanguage
{
    /// <summary>
    /// Creates a directive, like an import or a parameter.
    /// </summary>
    /// <param name="content">The content of the directive.</param>
    public string Context(string content);

    /// <summary>
    /// Creates code writing the result of the expression to the output.
    /// </summary>
    /// <param name="content">The C# expression.</param>
    public string InsertResult(string content);

    /// <summary>
    /// Creates code writing the XML escaped result of the expression to the output.
    /// </summary>
    /// <param name="content">The C# expression.</param>
    public string InsertResultEscaped(string content);

    /// <summary>
    /// Wraps the code in a feature block, which contains code instead of output.
    /// </summary>
    /// <param name="content">The C# code.</param>
    public MtCodeSnippet FeatureBlock(string content);

    /// <summary>
    /// Starts a feature block.
    /// </summary>
    public string FeatureBlockStart();

    /// <summary>
    /// Ends a feature block.
    /// </summary>
    public string FeatureBlockEnd();

    /// <summary>
    /// Simplifies the generated template without changing its output.
    /// </summary>
    /// <param name="generatedContent">The generated template.</param>
    public string OptimizeOutput(string generatedContent);
}