using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class LongResolver : TypeResolverBase<long>
    {
        public override string TypeName => "long";

        public override object Default => 0L;

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertInt64(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}