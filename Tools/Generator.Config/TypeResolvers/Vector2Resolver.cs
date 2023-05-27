using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class Vector2Resolver : TypeResolverBase<Vector2>
    {
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertVector2(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}