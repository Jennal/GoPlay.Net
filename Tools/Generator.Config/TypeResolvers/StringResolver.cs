using System.Text;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class StringResolver : TypeResolverBase<string>
    {
        public override string TypeName => "string";
        
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = value.Value.ToString();
            var bytes = Encoding.Default.GetBytes(val);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}