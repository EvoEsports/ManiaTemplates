using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ManiaTemplates.Lib;

public class ManiaLink
{
    private readonly Type _type;
    private readonly MethodInfo _renderMethod;

    public ManiaLink(string tag, string preCompiledTemplate, Assembly context)
    {
        var script = CSharpScript.Create(
            $@"{preCompiledTemplate} return typeof({tag});",
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

        return output?.Replace(@"pos=""0 0""", "")
            .Replace(@"size=""0 0""", "")
            .Replace(@"z-index=""0""", "")
            .Replace(@"scale=""1""", "")
            .Replace(@"rot=""0""", "")
            .Replace(@"scriptevents=""0""", "")
            .Replace(@"opacity=""1""", "")
            .Replace(@"valign=""top""", "")
            .Replace(@"halign=""left""", "")
            .Replace(@"textemboss=""0""", "")
            .Replace(@"autonewline=""0""", "")
            .Replace(@"textprefix=""""", "")
            .Replace(@"textcolor=""""", "")
            .Replace(@"focusareacolor1=""""", "")
            .Replace(@"focusareacolor2=""""", "")
            .Replace(@"maxline=""0""", "")
            .Replace(@"translate=""0""", "")
            .Replace(@"ScriptEvents=""0""", "")
            .Replace(@"textid=""""", "")
            .Replace(@"textfont=""""", "")
            .Replace(@"image=""""", "")
            .Replace(@"imagefocus=""""", "")
            .Replace(@"substyle=""""", "")
            .Replace(@"styleselected=""""", "")
            .Replace(@"colorize=""""", "")
            .Replace(@"modulatecolor=""""", "")
            .Replace(@"action=""""", "")
            .Replace(@"url=""""", "")
            .Replace(@"class=""""", "")
            .Replace(@"manialink=""""", "")
            .Replace(@"style=""""", "");
    }
}