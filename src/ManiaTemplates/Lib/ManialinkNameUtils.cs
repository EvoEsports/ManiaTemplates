using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

/// <summary>
/// Creates the names of manialinks from template keys.
/// </summary>
public static class ManialinkNameUtils
{
    private static readonly Regex ClassNameSlugifier = new(@"(?:[^a-zA-Z0-9]|mt$|xml$)");

    /// <summary>
    /// Converts a template key to the manialink id and class name, removing invalid characters and a trailing mt or xml.
    /// </summary>
    /// <param name="key">The key of the template.</param>
    public static string KeyToId(string key) =>
        "Mt" + ClassNameSlugifier.Replace(key, "");
}
