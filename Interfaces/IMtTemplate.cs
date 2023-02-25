namespace ManiaTemplates.Interfaces;

public interface IMtTemplate
{
    public Task<string> GetContent();
    public string GetXmlTag();
    public string? GetBasePath();
}