using OfficeOpenXml;
using GoPlay.Common.Data;

namespace GoPlay.Generators.Config
{
    public class RandGroupWithGuaranteedResolver : TypeResolverBase<RandGroupWithGuaranteed>
    {
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var (id, weight, guaranteed) = ExporterUtils.ConvertIFI(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString(), ExporterConsts.splitOutter);
            return new RandGroupWithGuaranteed
            {
                Id = id,
                Weight = weight,
                Guaranteed = guaranteed,
            };
        }
    }
}