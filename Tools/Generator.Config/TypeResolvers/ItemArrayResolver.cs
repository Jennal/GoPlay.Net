using OfficeOpenXml;
using GoPlay.Common.Data;

namespace GoPlay.Generators.Config
{
    public class ItemArrayResolver : TypeResolverBase<Item[]>
    {
        public override string TypeName => "Item[]";

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var result = new List<Item>();
            var arr = value.GetValue<string>().Split(ExporterConsts.splitOutter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in arr)
            {
                var val = ExporterUtils.ConvertVector2Int(sheet.Name, columnName, TypeName, value.End.Row, item, ExporterConsts.splitInner);
                result.Add(new Item
                {
                    Id = val.x,
                    Count = val.y,
                });
            }

            return result.ToArray();
            
        }
    }
}