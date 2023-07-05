// using OfficeOpenXml;
// using GoPlay.Common.Data;

// namespace GoPlay.Generators.Config
// {
//     public class RandGroupWithGuaranteedArrayResolver : TypeResolverBase<RandGroupWithGuaranteed[]>
//     {
//         public override string TypeName => "RandGroupWithGuaranteed[]";
        
//         public override string GetScriptClone(string fieldName)
//         {
//             return fieldName;
//         }

//         public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
//         {
//             var result = new List<RandGroupWithGuaranteed>();
//             var arr = value.GetValue<string>().Split(ExporterConsts.splitOutter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
//             foreach (var item in arr)
//             {
//                 var (id, weight, guaranteed) = ExporterUtils.ConvertIFI(sheet.Name, columnName, TypeName, value.End.Row, item, ExporterConsts.splitInner);
//                 result.Add(new RandGroupWithGuaranteed
//                 {
//                     Id = id,
//                     Weight = weight,
//                     Guaranteed = guaranteed,
//                 });
//             }

//             return result.ToArray();
            
//         }
//     }
// }