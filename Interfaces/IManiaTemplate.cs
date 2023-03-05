using ManiaTemplates.Components;

namespace ManiaTemplates.Interfaces;

public interface IManiaTemplate
{
    public Task<string> RenderAsync(dynamic data);
    public string Render(dynamic data);
    public void PreProcess();

    public MtComponent GetComponent();
}