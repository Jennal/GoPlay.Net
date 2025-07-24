using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class DecimalResolver : TypeResolverBase<decimal>
    {
        public override string TypeName => "decimal";

        public override object Default => 0f;

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertDecimal(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}