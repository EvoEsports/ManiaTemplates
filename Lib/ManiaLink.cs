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

    public string? Render(dynamic data)
    {
        var runnable = Activator.CreateInstance(_type);

        Type dataType = data.GetType();
        foreach (var dt in dataType.GetProperties())
        {
            _type.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
        }

        return (string?)_renderMethod.Invoke(runnable, null);
    }

    public string? RenderTimed(dynamic data)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var output = Render(data);
        stopwatch.Stop();
        var stopwatchElapsed = stopwatch.Elapsed;
        var renderTimeMs = Convert.ToInt32(stopwatchElapsed.TotalMilliseconds);
        Console.WriteLine($"Rendering took {renderTimeMs}ms.");
        return output;
    }
}