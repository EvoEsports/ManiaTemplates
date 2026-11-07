using System.Reflection;

namespace ManiaTemplates.Lib;

internal static class EmbeddedResourceUtils
{
    /// <summary>
    /// Gets the embedded resources contents of the given assembly.
    /// </summary>
    public static string GetEmbeddedResourceContent(string path, Assembly assembly)
    {
        // Embedded resources are read from the loaded assembly, so there is nothing to wait for
        using var stream = assembly.GetManifestResourceStream(path) ??
                           throw new InvalidOperationException("Could not load contents of: " + path);

        return new StreamReader(stream).ReadToEnd();
    }
}
