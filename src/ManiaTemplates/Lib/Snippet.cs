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

    public string ToString(string joinWith = "\n") =>
        string.Join(joinWith, this).Trim();
}
