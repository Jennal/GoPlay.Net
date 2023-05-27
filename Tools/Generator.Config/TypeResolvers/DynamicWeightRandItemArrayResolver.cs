using OfficeOpenXml;
using GoPlay.Common.Data;

namespace GoPlay.Generators.Config
{
    public class DynamicWeightRandItemArrayResolver : TypeResolverBase<DynamicWeightRandItem[]>
    {
        public override string TypeName => "DynamicWeightRandItem[]";
        
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var result = new List<DynamicWeightRandItem>();
            var arr = value.GetValue<string>().Split(ExporterConsts.splitOutter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in arr)
            {
                var (id, weightDefault, weightInc, weightMax) = ExporterUtils.ConvertIFFF(sheet.Name, columnName, TypeName, value.End.Row, item, ExporterConsts.splitInner);
                result.Add(new DynamicWeightRandItem
                {
                    Id = id,
                    WeightDefault = weightDefault,
                    WeightInc = weightInc,
                    WeightMax = weightMax,
                });
            }

            return result.ToArray();
            
        }
    }
}