using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    /// <summary>
    /// 支持5种格式：
    /// ff0000
    /// #ff0000
    /// ff0000ff
    /// #ff0000ff
    /// red
    /// </summary>
    public class ColorResolver : TypeResolverBase<Color>
    {
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertColor(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}