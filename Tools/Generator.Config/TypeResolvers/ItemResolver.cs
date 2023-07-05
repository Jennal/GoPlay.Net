// using OfficeOpenXml;
// using GoPlay.Common.Data;

// namespace GoPlay.Generators.Config
// {
//     public class ItemResolver : TypeResolverBase<Item>
//     {
//         public override string GetScriptClone(string fieldName)
//         {
//             return fieldName;
//         }

//         public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
//         {
//             var val = ExporterUtils.ConvertVector2Int(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
//             return new Item
//             {
//                 Id = val.x,
//                 Count = val.y,
//             };
//         }
//     }
// }