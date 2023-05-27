using System;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public abstract class TypeResolverBase
    {
        public abstract object Default { get; }
        public abstract string TypeName { get; }
        public abstract string Namespace { get; }
        public abstract Type Type { get; }

        public virtual bool IsEmpty(ExcelWorksheet sheet, ExcelRangeBase value)
        {
            if (value.Value == null) return true;
            return ExporterUtils.IsEmptyColumn(value.Value.ToString());
        }

        public virtual bool RecognizeType(string typeName)
        {
            return string.Equals(typeName, TypeName, StringComparison.OrdinalIgnoreCase);
        }

        public virtual string GetScriptClone(string fieldName)
        {
            return $"({TypeName}){fieldName}.Clone()";
        }
        
        public abstract object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value);
    }
    
    public abstract class TypeResolverBase<T> : TypeResolverBase
    {
        public override object Default => default(T);
        public override string TypeName => typeof(T).Name;
        public override string Namespace => Type.Namespace;
        public override Type Type => typeof(T);
    }
}