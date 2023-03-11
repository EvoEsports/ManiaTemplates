using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ManiaTemplates.Lib;

public class ManiaLink
{
    private readonly string _className;
    private readonly string _preCompiledTemplate;
    private readonly IEnumerable<Assembly> _assemblies;
    private readonly Type _textTransformer;

    private static readonly Regex ReplaceDefaultAttr =
        new(
            @"\s(\w[\w0-9]*=""\s*""|(pos|size)=""0 0""|(z-index|rot|scriptevents|textemboss|autonewline|maxline|translate)=""0""|(scale|opacity)=""1""|valign=""top""|halign=""left"")",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Creates an ManiaLink-instance from tag & pre-compiled template with given context.
    /// </summary>
    public ManiaLink(string className, string preCompiledTemplate, IEnumerable<Assembly> assemblies)
    {
        _preCompiledTemplate = preCompiledTemplate;
        _assemblies = assemblies;
        _className = className;
        _textTransformer = CompileRenderScript();
    }

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    /// <returns>
    /// A string containing the rendered manialink, ready to be displayed.
    /// </returns>
    public string? Render(dynamic data)
    {
        var runnable = Activator.CreateInstance(_textTransformer);

        Type dataType = data.GetType();
        foreach (var dt in dataType.GetProperties())
        {
            _textTransformer.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
        }

        var method = _textTransformer.GetMethod("TransformText");
        var output = (string?)method?.Invoke(runnable, null);

        return output == null ? null : ReplaceDefaultAttr.Replace(output, "");
    }

    [Obsolete("Render(data, assemblies) is deprecated, please use Render(data) instead.")]
    public string? Render(dynamic data, IEnumerable<Assembly> assemblies)
    {
        return Render(data);
    }

    /// <summary>
    /// Create empty manialink for this instance, used to hide displayed UI elements.
    /// </summary>
    /// <returns>
    /// A string containing the empty manialink, ready to be send to the player(s).
    /// </returns>
    public string Hide()
    {
        return @$"<manialink id=""{_className}""></manialink>";
    }

    /// <summary>
    /// Compiles the template script that generates the output for given data.
    /// </summary>
    private Type CompileRenderScript()
    {
        var options = ScriptOptions.Default
            .WithReferences(typeof(ManiaLink).Assembly)
            .WithReferences(_assemblies);

        var code = $"{_preCompiledTemplate} return typeof({_className});";
        var script = CSharpScript.Create(code, options);
        script.Compile();

        var type = (Type)script.RunAsync().Result.ReturnValue;
        var method = type.GetMethod("TransformText");
        if (method == null)
        {
            throw new Exception("Missing method 'TransformText' in compiled render script.");
        }

        return type;
    }
}