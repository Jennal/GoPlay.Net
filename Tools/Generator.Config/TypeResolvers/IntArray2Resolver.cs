using GoPlay.Common.Data;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class IntArray2Resolver : TypeResolverBase<int[][]>
    {
        public override string TypeName => "int[][]";
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var content = value.Value.ToString();
            content = content.Replace("\r", "");
            content = content.Replace("\n", "");
            var val = ExporterUtils.ConvertInt32Array2(sheet.Name, columnName, TypeName, value.End.Row, content);
            return val;
        }
    }
}