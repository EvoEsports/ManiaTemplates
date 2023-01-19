namespace ManiaTemplates.Lib;

public class ComponentAttributes
{
    private Dictionary<string, string> _attributes;

    public ComponentAttributes()
    {
        _attributes = new();
    }

    public void Add(string name, string value)
    {
        _attributes.Add(name, value);
    }

    public bool Has(string name)
    {
        return _attributes.ContainsKey(name);
    }

    public string Get(string name)
    {
        return _attributes[name];
    }

    public Dictionary<string, string> All()
    {
        return _attributes;
    }

    public string GetHash()
    {
        var sortedDict = from attribute in _attributes orderby attribute.Value select attribute;
        var concatenatedAttributes = "";

        foreach (var (attributeName, attributeValue) in sortedDict)
        {
            concatenatedAttributes += $"{attributeName}={attributeValue}";
        }

        return Helper.Hash(concatenatedAttributes);
    }
}