using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class Vector3IntResolver : TypeResolverBase<Vector3Int>
    {
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertVector3Int(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}