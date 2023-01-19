namespace ManiaTemplates.Lib;

public class Snippet
{
    private const int IndentationMultiplier = 2;
    private readonly int _currentIndentation;
    private readonly List<string> _output = new();

    public Snippet(int currentIndentation = 0)
    {
        _currentIndentation = currentIndentation;
    }

    public Snippet AppendLine(string str)
    {
        _output.Add(str);
        return this;
    }

    public Snippet AppendLine(int? addIndent, string str)
    {
        if (addIndent != null)
        {
            _output.Add(Indentation((int)addIndent) + str);
            return this;
        }

        _output.Add(str);
        return this;
    }

    public Snippet AppendSnippet(int? addIndent, Snippet content)
    {
        foreach (var line in content._output)
        {
            AppendLine(addIndent, line);
        }

        return this;
    }

    public Snippet AppendSnippet(Snippet content)
    {
        foreach (var line in content._output)
        {
            AppendLine(line);
        }

        return this;
    }

    private string Indentation(int additionalIndentation)
    {
        return new string(' ', (_currentIndentation + additionalIndentation) * IndentationMultiplier);
    }

    public string ToString(string joinWith = "\n")
    {
        return string.Join(joinWith, _output).Trim();
    }
}