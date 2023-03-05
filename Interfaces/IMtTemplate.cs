namespace ManiaTemplates.Interfaces;

public interface IMtTemplate
{
    public Task<string> GetContent();
}