using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ManiaTemplates.Lib;

public class ManiaLink
{
    private readonly string _className;
    private readonly Type _textTransformer;

    // Matches attributes with default values, script blocks are matched as well to skip them, as scripts may contain
    // the same text as code
    private static readonly Regex ReplaceDefaultAttr =
        new(
            @"(?s)(<script>.*?</script>)|\s(\w[\w0-9]*=""\s*""|(pos|size)=""0 0""|(z-index|rot|scriptevents|textemboss|autonewline|maxline|translate)=""0""|(scale|opacity)=""1""|valign=""top""|halign=""left"")",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// Creates an ManiaLink-instance from tag & pre-compiled template with given context.
    /// </summary>
    public ManiaLink(string className, string preCompiledTemplate, IEnumerable<Assembly> assemblies)
    {
        _className = className;
        _textTransformer = TemplateCompiler.Compile(className, preCompiledTemplate, assemblies);
    }

    public Task<string> RenderAsync(object data, IDictionary<string, object?>? globalVariables = null)
    {
        var runnable = GetTransformerinstance();
        var dataType = data.GetType();
        AddGlobalVariablesToTextTransformer(runnable, globalVariables);
        foreach (var dt in dataType.GetProperties())
        {
            _textTransformer.GetProperty(dt.Name)?.SetValue(runnable, dt.GetValue(data));
        }

        return RenderInternalAsync(runnable);
    }

    public Task<string> RenderAsync(IDictionary<string, object?> data, IDictionary<string, object?>? globalVariables = null)
    {
        var runnable = GetTransformerinstance();
        AddGlobalVariablesToTextTransformer(runnable, globalVariables);
        foreach (var (key, value) in data)
        {
            _textTransformer.GetProperty(key)?.SetValue(runnable, value);
        }

        return RenderInternalAsync(runnable);
    }

    private void AddGlobalVariablesToTextTransformer(object? runnable, IDictionary<string, object?>? globalVariables = null)
    {
        if (runnable == null || globalVariables == null)
        {
            return;
        }

        foreach (var (key, value) in globalVariables)
        {
            _textTransformer.GetProperty(key)?.SetValue(runnable, value);
        }
    }

    public Task<string> RenderAsync(ExpandoObject data) => RenderAsync((IDictionary<string, object?>)data);

    private object? GetTransformerinstance() => Activator.CreateInstance(_textTransformer);

    /// <summary>
    /// Render the manialink instance with the given data.
    /// </summary>
    /// <returns>
    /// A string containing the rendered manialink, ready to be displayed.
    /// </returns>
    private Task<string> RenderInternalAsync(object? runnable)
    {
        var method = _textTransformer.GetMethod("TransformText");
        // Errors of the template are thrown as they are, instead of wrapped in a TargetInvocationException
        var output = (string?)method?.Invoke(runnable, BindingFlags.DoNotWrapExceptions, null, null, null);

        var result = output == null
            ? ""
            : ReplaceDefaultAttr.Replace(output, match => match.Groups[1].Success ? match.Value : "");
        return Task.FromResult(result);
    }

    /// <summary>
    /// Create empty manialink for this instance, used to hide displayed UI elements.
    /// </summary>
    /// <returns>
    /// A string containing the empty manialink, ready to be send to the player(s).
    /// </returns>
    public string Hide() => CreateHideManialink(_className);

    /// <summary>
    /// Create empty manialink with the given id, used to hide displayed UI elements.
    /// </summary>
    public static string CreateHideManialink(string id)
    {
        return @$"<manialink id=""{id}""></manialink>";
    }

    /// <summary>
    /// Creates ManiaLink opening tag with version, name and id.
    /// 
    /// id = Identifies the ManiaLink for overwrite/delete.
    /// name = Shown in in-game debugger.
    /// version = Version for the markup language of Trackmania.
    /// </summary>
    public static string OpenTag(string name, int version = 3, string? displayLayer = null)
    {
        var layer = "";
        if (displayLayer != null)
        {
            layer += $@" layer=""{displayLayer}""";
        }

        return $@"<manialink version=""{version}"" id=""{name}"" name=""EvoSC#-{name}""{layer}>";
    }

    /// <summary>
    /// Creates ManiaLink closing tag.
    /// </summary>
    public static string CloseTag()
    {
        return "</manialink>";
    }
}