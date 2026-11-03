namespace ManiaTemplates.Lib;

public class MtCodeSnippet : List<string>
{
    public MtCodeSnippet AppendLine(string str)
    {
        Add(str);
        return this;
    }

    public MtCodeSnippet AppendSnippet(MtCodeSnippet content)
    {
        foreach (var line in content)
        {
            AppendLine(line);
        }

        return this;
    }

    public override string ToString() => ToString("\n");

    public string ToString(string joinWith) => string.Join(joinWith, this).Trim();
}
