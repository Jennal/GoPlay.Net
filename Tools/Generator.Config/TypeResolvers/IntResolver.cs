using System;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class IntResolver : TypeResolverBase<int>
    {
        public override string TypeName => "int";

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertInt32(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}