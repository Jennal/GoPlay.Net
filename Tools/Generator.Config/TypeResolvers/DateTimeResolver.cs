using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class DateTimeResolver : TypeResolverBase<DateTime>
    {   
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertDateTime(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}