using System.Reflection;

namespace ManiaTemplates.Lib;

/// <summary>
/// Helpers for reading embedded resources.
/// </summary>
internal static class EmbeddedResourceUtils
{
    /// <summary>
    /// Gets the embedded resources contents of the given assembly.
    /// </summary>
    /// <param name="path">The name of the embedded resource.</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    public static string GetEmbeddedResourceContent(string path, Assembly assembly)
    {
        // Embedded resources are read from the loaded assembly, so there is nothing to wait for
        using var stream = assembly.GetManifestResourceStream(path) ??
                           throw new InvalidOperationException("Could not load contents of: " + path);

        return new StreamReader(stream).ReadToEnd();
    }
}
