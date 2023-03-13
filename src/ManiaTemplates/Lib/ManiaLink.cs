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
    private Type _textTransformer;

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
    }

    internal async Task CompileAsync() => _textTransformer = await CompileRenderScriptAsync();

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    /// <returns>
    /// A string containing the rendered manialink, ready to be displayed.
    /// </returns>
    public string? Render(dynamic data)
    {
        return RenderAsync(data).Result;
    }

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    /// <returns>
    /// A string containing the rendered manialink, ready to be displayed.
    /// </returns>
    public async Task<string> RenderAsync(dynamic data)
    {
        if (_textTransformer == null)
        {
            throw new InvalidOperationException("Text transformer not initialized, call CompileAsync first.");
        }
        
        return await Task.Run(delegate
        {
            var runnable = Activator.CreateInstance(_textTransformer);

            Type dataType = data.GetType();
            foreach (var dt in dataType.GetProperties())
            {
                _textTransformer.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
            }

            var method = _textTransformer.GetMethod("TransformText");
            var output = (string?)method?.Invoke(runnable, null);

            return output == null ? "" : ReplaceDefaultAttr.Replace(output, "");
        });
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
    private async Task<Type> CompileRenderScriptAsync()
    {
        var options = ScriptOptions.Default
            .WithReferences(typeof(ManiaLink).Assembly)
            .WithReferences(_assemblies);

        var code = $"{_preCompiledTemplate} return typeof({_className});";
        var script = CSharpScript.Create(code, options);
        script.Compile();

        var result = await script.RunAsync();
        var type = result.ReturnValue as Type;
        var method = type?.GetMethod("TransformText");
        
        if (type == null || method == null)
        {
            throw new InvalidOperationException("Missing method 'TransformText' in compiled render script.");
        }

        return type;
    }
}
