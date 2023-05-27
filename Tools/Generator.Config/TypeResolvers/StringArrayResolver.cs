using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class StringArrayResolver : TypeResolverBase<string[]>
    {
        public override string TypeName => "string[]";
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertStringArray(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}