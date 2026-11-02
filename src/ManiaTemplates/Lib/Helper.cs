using System.Reflection;

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
}
