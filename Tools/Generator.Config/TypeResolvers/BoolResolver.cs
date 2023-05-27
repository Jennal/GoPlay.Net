using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class BoolResolver : TypeResolverBase<bool>
    {
        public override string TypeName => "bool";

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertBool(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}