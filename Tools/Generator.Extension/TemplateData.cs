using DotLiquid;

namespace GoPlay.Generators.Extension
{
    [LiquidType("*")]
    public class PushData
    {
        public string name { get; set; }
        public string route { get; set; }
    }
    
    [LiquidType("*")]
    public class TemplateData
    {
        public string route { get; set; }
        public string method { get; set; }
        public string returnType { get; set; }
        public string paramType { get; set; }
        public bool isNeedLogin { get; set; }
    }
}