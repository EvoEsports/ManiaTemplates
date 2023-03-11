using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ManiaTemplates.Lib;

public class ManiaLink
{
    private readonly string _className;
    private readonly string _preCompiledTemplate;

    private static readonly Regex ReplaceDefaultAttr =
        new(
            @"\s(\w[\w0-9]*=""\s*""|(pos|size)=""0 0""|(z-index|rot|scriptevents|textemboss|autonewline|maxline|translate)=""0""|(scale|opacity)=""1""|valign=""top""|halign=""left"")",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Creates an ManiaLink-instance from tag & pre-compiled template with given context.
    /// </summary>
    public ManiaLink(string className, string preCompiledTemplate)
    {
        _preCompiledTemplate = preCompiledTemplate;
        _className = className;
    }

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    /// <returns>
    /// A string containing the rendered manialink, ready to be displayed.
    /// </returns>
    public string? Render(dynamic data, IEnumerable<Assembly> assemblies)
    {
        var options = ScriptOptions.Default
            .WithReferences(typeof(ManiaLink).Assembly)
            .WithReferences(assemblies);
        
        var code = $"{_preCompiledTemplate} return typeof({_className});";
        var script = CSharpScript.Create(code, options);
        script.Compile();

        var type = (Type)script.RunAsync().Result.ReturnValue;
        var method = type.GetMethod("TransformText");
        if (method == null)
        {
            throw new Exception("Missing method 'TransformText' in pre-compiled script.");
        }

        var runnable = Activator.CreateInstance(type);

        Type dataType = data.GetType();
        foreach (var dt in dataType.GetProperties())
        {
            type.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
        }

        var output = (string?)method.Invoke(runnable, null);

        return output == null ? null : ReplaceDefaultAttr.Replace(output, "");
    }

    /// <summary>
    /// Create empty manialink for this instance, used to hide displayed UI elements.
    /// </summary>
    /// <returns>
    /// A string containing the empty manialink, ready to be send to the player(s).
    /// </returns>
    public string Hide()
    {
        return @$"<manialink version=""3"" id=""{_className}""></manialink>";
    }
}
