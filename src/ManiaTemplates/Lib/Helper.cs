using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ManiaTemplates.Lib;

public abstract class Helper
{
    /// <summary>
    /// Gets the embedded resources contents of the given assembly.
    /// </summary>
    public static async Task<string> GetEmbeddedResourceContentAsync(string path, Assembly assembly)
    {
        await using var stream = assembly.GetManifestResourceStream(path) ??
                                 throw new InvalidOperationException("Could not load contents of: " + path);

        return await new StreamReader(stream).ReadToEndAsync();
    }

    /// <summary>
    /// Takes a XML-string and aligns all nodes properly.
    /// </summary>
    public static string PrettyXml(string? uglyXml = null)
    {
        if (uglyXml == null || uglyXml.Trim().Length == 0)
        {
            return "";
        }

        var stringBuilder = new StringBuilder();
        var element = XElement.Parse(uglyXml);

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        settings.NewLineOnAttributes = false;

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }
}
