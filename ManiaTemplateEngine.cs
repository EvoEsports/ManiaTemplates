using System.Reflection;
using ManiaTemplates.Components;
using ManiaTemplates.Languages;
using ManiaTemplates.Lib;
using Mono.TextTemplating;

namespace ManiaTemplates;

public class ManiaTemplateEngine
{
    private readonly IMtTargetLanguage _mtTargetLanguage = new MtLanguageT4();
    protected internal MtComponentList BaseMtComponents { get; } = new();

    public ManiaTemplateEngine()
    {
        LoadCoreComponents();
    }

    public ManiaLink PreProcess(MtComponent mtComponent)
    {
        var t4Template = ConvertComponent(mtComponent);
        var ttFilename = $"{mtComponent.Tag}.tt";

        File.WriteAllText(ttFilename, t4Template);

        var generator = new TemplateGenerator();
        var parsedTemplate = generator.ParseTemplate(ttFilename, t4Template);
        var templateSettings = TemplatingEngine.GetSettings(generator, parsedTemplate);

        templateSettings.CompilerOptions = "-nullable:enable";
        templateSettings.Name = mtComponent.Tag;
        templateSettings.Namespace = "ManiaTemplate";

        File.WriteAllText("../../../Debug.tt", t4Template);

        var preCompiledTemplate =
            generator.PreprocessTemplate(parsedTemplate, ttFilename, t4Template, templateSettings,
                out string[] loadedAssemblies);

        //Remove namespace wrapper
        preCompiledTemplate = preCompiledTemplate.Replace("namespace ManiaTemplate {", "")[..^5];

        return new ManiaLink(mtComponent.Tag, preCompiledTemplate, Assembly.GetCallingAssembly());
    }

    private void LoadCoreComponents()
    {
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Components/Pagination.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Graph.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Label.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/Quad.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/ManiaLink/RoundedQuad.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Wrapper/Frame.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Wrapper/Widget.mt"));
        AddComponentFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates/Wrapper/Window.mt"));
    }

    private void AddComponentFile(string templateFile, string? importAs = null)
    {
        var template = new TemplateFile(templateFile);
        var component = MtComponent.FromTemplate(template, importAs);
        BaseMtComponents.Add(importAs ?? template.Name, component);
    }

    private string ConvertComponent(MtComponent mtComponent)
    {
        return new Transformer(this, _mtTargetLanguage).BuildManialink(mtComponent);
    }

    public string GenerateComponentsMarkdown()
    {
        var componentMdGenerator = new MtComponentMarkdownGenerator(BaseMtComponents);
        return componentMdGenerator.Generate();
    }
}