namespace ManiaTemplates.Lib;

public class Snippet : List<string>
{
    public Snippet AppendLine(string str)
    {
        Add(str);
        return this;
    }

    public Snippet AppendSnippet(Snippet content)
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
