using System.Numerics;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class BigIntegerResolver : TypeResolverBase<BigInteger>
    {
        public override string TypeName => "BigInteger";

        public override bool RecognizeType(string typeName)
        {
            var typeNames = new[]
            {
                "BigInt",
                "BigInteger",
                "ObscuredBigInteger",
                "ObscuredBigInt",
            };

            foreach (var tn in typeNames)
            {
                if (string.Equals(typeName, tn, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            var val = ExporterUtils.ConvertBigInteger(sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString());
            return val;
        }
    }
}