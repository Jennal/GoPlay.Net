using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class FloatResolver : TypeResolverBase<float>
    {
        public override string TypeName => "float";
        
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertFloat(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}