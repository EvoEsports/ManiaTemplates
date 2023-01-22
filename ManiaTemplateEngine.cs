using System.Reflection;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

public class ManiaTemplateEngine
{
    private readonly ITargetLanguage _targetLanguage = new T4();
    protected internal ComponentList BaseComponents { get; } = new();

    public ManiaTemplateEngine()
    {
        LoadCoreComponents();
    }

    public ManiaTemplateEngine(ITargetLanguage targetLanguage)
    {
        _targetLanguage = targetLanguage;
        LoadCoreComponents();
    }

    private void LoadCoreComponents()
    {
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Components/Image.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Label.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Quad.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/RoundedQuad.mt"));
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

    public ManiaLink PreProcess(Component component)
    {
        var t4Template = ConvertComponent(component);
        var ttFilename = $"{component.Tag}.tt";

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);
        var references = Array.Empty<string>();

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = component.Tag;
        templateSettings.Namespace = "ManiaTemplate";

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings, out references);

        Console.WriteLine("Loaded assemblies:\n- " + string.Join("\n- ", references));

        //Remove namespace wrapper
        preCompiledTemplate = preCompiledTemplate.Replace("namespace ManiaTemplate {", "")[..^5];

        return new ManiaLink(component.Tag, preCompiledTemplate, Assembly.GetCallingAssembly());
    }
}