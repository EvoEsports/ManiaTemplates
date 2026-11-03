using ManiaTemplates;

// Renders the template passed as argument in a plain process, which only loads the assemblies the engine needs,
// unlike the test host
var engine = new ManiaTemplateEngine();
engine.AddTemplateFromString("Template", args[0]);

try
{
    Console.Write(await engine.RenderAsync("Template", new { }));
}
catch (Exception exception)
{
    Console.Write(exception.Message);
    return 1;
}

return 0;
