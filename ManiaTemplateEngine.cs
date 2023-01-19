using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

public class ManiaTemplateEngine
{
    private readonly ITargetLanguage _targetLanguage;
    protected internal ComponentList BaseComponents { get; }

    public ManiaTemplateEngine(ITargetLanguage targetLanguage)
    {
        _targetLanguage = targetLanguage;
        BaseComponents = new ComponentList();

        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Components/Image.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Label.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Quad.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Wrapper/Frame.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Wrapper/Window.mt"));
    }

    private void AddComponentFile(string templateFile, string? importAs = null)
    {
        var template = new TemplateFile(templateFile);
        var component = Component.FromTemplate(template, importAs);
        BaseComponents.Add(importAs ?? template.Name, component);
    }

    public string ConvertComponent(Component component)
    {
        return new Transformer(this, _targetLanguage).BuildManialink(component);
    }

    public async Task<string?> Render(string t4Template, dynamic data)
    {
        var generator = new TemplateGenerator();

        //Parse template
        var parsedTemplate = generator.ParseTemplate("pseudo.txt", t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        //Provide data
        var session = generator.GetOrCreateSession();
        Type type = data.GetType();
        foreach (var propertyInfo in type.GetProperties())
        {
            session[propertyInfo.Name] = propertyInfo.GetValue(data);
        }

        var (generatedFilename, generatedContent) = await generator.ProcessTemplateAsync(
            parsedTemplate, "pseudo.txt", t4Template, "pseudo.out.xml", templateSettings
        );

        return generatedContent;
    }
}