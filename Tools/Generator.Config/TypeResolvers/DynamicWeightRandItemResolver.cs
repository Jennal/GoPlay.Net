// using OfficeOpenXml;
// using GoPlay.Common.Data;

// namespace GoPlay.Generators.Config
// {
//     public class DynamicWeightRandItemResolver : TypeResolverBase<DynamicWeightRandItem>
//     {
//         public override string GetScriptClone(string fieldName)
//         {
//             return fieldName;
//         }

//         public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
//         {
//             var (id, weightDefault, weightInc, weightMax) = ExporterUtils.ConvertIFFF(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString(), ExporterConsts.splitOutter);
//             return new DynamicWeightRandItem
//             {
//                 Id = id,
//                 WeightDefault = weightDefault,
//                 WeightInc = weightInc,
//                 WeightMax = weightMax,
//             };
//         }
//     }
// }