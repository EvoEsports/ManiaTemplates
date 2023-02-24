using System.Collections;

namespace ManiaTemplates.Lib;

public class Snippet : List<string>
{
    private const int IndentationMultiplier = 0;
    private readonly int _currentIndentation;

    public Snippet(int currentIndentation = 0)
    {
        _currentIndentation = currentIndentation;
    }

    public Snippet AppendLine(string str)
    {
        Add(str);
        return this;
    }

    public Snippet AppendLine(int? addIndent, string str)
    {
        if (addIndent != null)
        {
            AppendLine(Indentation((int)addIndent) + str);
            return this;
        }

        AppendLine(str);
        return this;
    }

    public Snippet AppendSnippet(int? addIndent, Snippet content)
    {
        foreach (var line in content)
        {
            AppendLine(addIndent, line);
        }

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

    private string Indentation(int additionalIndentation) =>
        new(' ', (_currentIndentation + additionalIndentation) * IndentationMultiplier);

    public string ToString(string joinWith = "\n") =>
        string.Join(joinWith, this).Trim();
}