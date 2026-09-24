using DotLiquid;

namespace GoPlay.Generators.Config;

[LiquidType("*")]
public class TemplateField
{
    public string typeName { get; set; }
    public string name { get; set; }
    public string[] desc { get; set; }
    public bool isArray { get; set; }
    public string privateName { get; set; }
}

[LiquidType("*")]
public class TemplateData
{
    public string excelFile { get; set; }
    public string tableName { get; set; }
    public string[] tableDesc { get; set; }
    public string entityName { get; set; }
    
    public List<string> namespaces { get; set; }
    public List<TemplateField> fields { get; set; }
    public List<TemplateIndex> indexes { get; set; } = new List<TemplateIndex>();
}

/// <summary>
/// 一个索引组（k / k1 / k2 ...）生成的查询接口：TryGetBy{name}({parameters}, out {entity} {resultName}, string {langName})
/// </summary>
[LiquidType("*")]
public class TemplateIndex
{
    public string label { get; set; }
    public string name { get; set; }
    public string keyType { get; set; }
    public string parameters { get; set; }
    public string parameterKey { get; set; }
    public string valueKey { get; set; }
    public string resultName { get; set; }
    public string langName { get; set; }
}

[LiquidType("*")]
public class TemplateEnumField
{
    public string[] desc { get; set; }
    public string name { get; set; }
    public string value { get; set; }
}

[LiquidType("*")]
public class TemplateEnumData
{
    public string excelFile { get; set; }
    public string tableName { get; set; }
    public string[] desc { get; set; }
    public string name { get; set; }
    public bool isFlags { get; set; }
    
    public List<TemplateEnumField> fields { get; set; }
}