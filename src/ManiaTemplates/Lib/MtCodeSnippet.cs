namespace ManiaTemplates.Lib;

/// <summary>
/// Lines of generated code.
/// </summary>
public class MtCodeSnippet : List<string>
{
    /// <summary>
    /// Adds a line and returns this snippet.
    /// </summary>
    /// <param name="str">The line to add.</param>
    public MtCodeSnippet AppendLine(string str)
    {
        Add(str);
        return this;
    }

    /// <summary>
    /// Adds the lines of another snippet and returns this snippet.
    /// </summary>
    /// <param name="content">The snippet to add.</param>
    public MtCodeSnippet AppendSnippet(MtCodeSnippet content)
    {
        AddRange(content);
        return this;
    }

    public override string ToString() => ToString("\n");

    /// <summary>
    /// Joins the lines with the given separator and trims the result.
    /// </summary>
    /// <param name="joinWith">The separator between the lines.</param>
    public string ToString(string joinWith) => string.Join(joinWith, this).Trim();
}
