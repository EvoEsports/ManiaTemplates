using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

public class TemplateFile
{
    public string TemplatePath { get; }
    public string Name { get; }
    public DateTime LastModification { get; }

    public TemplateFile(string filename)
    {
        TemplatePath = filename;
        Name = Regex.Replace(Path.GetFileName(TemplatePath), @"^(.+)\.\w+$", "$1");
        LastModification = File.GetLastWriteTime(filename);
    }

    public string Content()
    {
        return File.ReadAllText(TemplatePath);
    }

    public string? Directory()
    {
        return Path.GetDirectoryName(TemplatePath);
    }
}