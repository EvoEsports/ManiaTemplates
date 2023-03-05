using System.Reflection;
using ManiaTemplates.Components;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

//TODO: Better naming
public class LoadedTemplate : IManiaTemplate
{
    public required ManiaTemplateEngine Engine;
    public required MtComponent Component;
    public required Assembly SourceAssembly;
    public required string ResourceKey;

    private ManiaLink? _preProcessedTemplate;

    public async Task<string> RenderAsync(dynamic data)
    {
        return await Task.Run(() => Render(data));
    }

    public string Render(dynamic data)
    {
        if (_preProcessedTemplate == null)
        {
            PreProcess();
        }

        if (_preProcessedTemplate == null)
        {
            throw new Exception("Failed to render template: " + ResourceKey);
        }

        return _preProcessedTemplate.Render(data);
    }

    public void PreProcess()
    {
        var className = string.Join("", ResourceKey.Split('.')[..^1]);
        _preProcessedTemplate = Engine.PreProcess(Component, SourceAssembly, className);
    }

    public MtComponent GetComponent()
    {
        return Component;
    }
}