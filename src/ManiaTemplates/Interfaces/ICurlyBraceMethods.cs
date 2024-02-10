using System.Text.RegularExpressions;

namespace ManiaTemplates.Interfaces;

public interface ICurlyBraceMethods
{
    protected static readonly Regex TemplateInterpolationRegex = new(@"\{\{\s*(.+?)\s*\}\}");

    /// <summary>
    /// Takes the contents of double curly braces in a string and wraps them into something else. The second Argument takes a string-argument and returns the newly wrapped string.
    /// </summary>
    public string ReplaceCurlyBraces(string value, Func<string, string> curlyContentWrapper);

    /// <summary>
    /// Checks whether double interpolation exists ({{ {{ a }} {{ b }} }}) and throws exception if so.
    /// </summary>
    public void PreventInterpolationRecursion(string value);

    /// <summary>
    /// Checks whether double interpolation exists ({{ {{ a }} {{ b }} }}) and throws exception if so.
    /// </summary>
    public void PreventCurlyBraceCountMismatch(string value);
}