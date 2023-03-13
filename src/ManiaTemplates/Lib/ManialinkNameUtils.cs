using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

public static class ManialinkNameUtils
{
    private static readonly Regex ClassNameSlugifier = new(@"(?:[^a-zA-Z0-9]|mt$|xml$)");

    public static string KeyToId(string key) =>
        "Mt" + ClassNameSlugifier.Replace(key, "");

    public static string KeyToName(string key) => $"EvoSC#-{KeyToId(key)}";
}
