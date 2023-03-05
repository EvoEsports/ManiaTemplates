using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ManiaTemplates.Lib;

public class ManiaLink
{
    private readonly Type _type;
    private readonly MethodInfo _renderMethod;

    private static readonly Regex ReplaceDefaultAttr =
        new(
            @"\s(\w[\w0-9]*=""\s*""|(pos|size)=""0 0""|(z-index|rot|scriptevents|textemboss|autonewline|maxline|translate)=""0""|(scale|opacity)=""1""|valign=""top""|halign=""left"")",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Creates an ManiaLink-instance from tag & pre-compiled template with given context.
    /// </summary>
    public ManiaLink(string className, string preCompiledTemplate, Assembly context)
    {
        var script = CSharpScript.Create(
            $@"{preCompiledTemplate} return typeof({className});",
            ScriptOptions.Default.WithReferences(context)
        );

        script.Compile();

        _type = (Type)script.RunAsync().Result.ReturnValue;
        var method = _type.GetMethod("TransformText");
        if (method == null)
        {
            throw new Exception("Missing method 'TransformText' in pre-compiled script.");
        }

        _renderMethod = method;
    }

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    public string? Render(dynamic data)
    {
        var runnable = Activator.CreateInstance(_type);

        Type dataType = data.GetType();
        foreach (var dt in dataType.GetProperties())
        {
            _type.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
        }

        var output = (string?)_renderMethod.Invoke(runnable, null);

        return output == null ? null : ReplaceDefaultAttr.Replace(output, "");
    }
}