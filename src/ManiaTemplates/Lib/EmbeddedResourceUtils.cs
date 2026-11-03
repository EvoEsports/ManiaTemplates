using System.Reflection;

namespace ManiaTemplates.Lib;

internal static class EmbeddedResourceUtils
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
}
