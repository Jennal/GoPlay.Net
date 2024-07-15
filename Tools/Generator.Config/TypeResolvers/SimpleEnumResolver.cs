using System.Reflection;
using OfficeOpenXml;

namespace GoPlay.Generators.Config {
    public class SimpleEnumTypeResolver : TypeResolverBase
    {
        public override object Default
        {
            get
            {
                if (_isArray) return null;
                return 0;
            }
        }
        public override string TypeName => string.IsNullOrEmpty(_enumTypeName) ? _enumType?.ToString() : _enumTypeName;
        public override string Namespace => _enumType != null ? _enumType.Namespace : "GoPlay.Config";
        public override Type Type => _enumType;

        private bool _isArray;
        private Type _enumType;
        private static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static Dictionary<string, Type> _fullNametypeCache = new Dictionary<string, Type>();

        private string _enumTypeName;
        private static Dictionary<string, List<(string, int)>> _exportedEnum => Excel2Enum.Enums;

        static SimpleEnumTypeResolver()
        {
//            var start = Time.realtimeSinceStartup;
            var list = ReflectionHelper.GetTypesInAllLoadedAssemblies(o => o.IsEnum);
            foreach (var type in list)
            {
                var name = type.Name;
                var fullName = type.Name;
                if (!string.IsNullOrEmpty(type.Namespace)) fullName = $"{type.Namespace}.{name}";

                _typeCache[name] = type;
                _fullNametypeCache[fullName] = type;
            }
//            Debug.Log($"===========> Simple Enum time: {Time.realtimeSinceStartup - start}");
        }
        
        public override string GetScriptClone(string fieldName)
        {
            return fieldName;
        }
        
        public override bool RecognizeType(string typeName)
        {
            _isArray = false;
            
            if (typeName.EndsWith("[]"))
            {
                _isArray = true;
                typeName = typeName.Substring(0, typeName.Length - 2);
            }

            if (_exportedEnum.ContainsKey(typeName))
            {
                _enumTypeName = typeName;
                return true;
            }
            
            _enumType = null;
            if (_typeCache.ContainsKey(typeName))
            {
                _enumType = _typeCache[typeName];
            }
            
            if (_fullNametypeCache.ContainsKey(typeName))
            {
                _enumType = _fullNametypeCache[typeName];
            }

            if (_enumType != null) _enumTypeName = _enumType.Name;
            
            return _enumType != null;
        }

        public override object GetValue(ExcelWorksheet sheet, string columnName, ExcelRangeBase value)
        {
            if (_exportedEnum.ContainsKey(_enumTypeName))
            {
                var val = value.GetValue<string>();
                if (_isArray)
                {
                    var arr = val.Split(ExporterConsts.splitInner.ToCharArray());
                    var result = new int[arr.Length];
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var item = _exportedEnum[_enumTypeName].FirstOrDefault(o => o.Item1 == val);
                        if (item != default)
                        {
                            result[i] = item.Item2;
                        }
                        else
                        {
                            result[i] = ExporterUtils.ConvertInt32(sheet.Name, columnName, _enumTypeName, value.End.Row, val);
                        }
                    }

                    return result;
                }
                else
                {
                    var item = _exportedEnum[_enumTypeName].FirstOrDefault(o => o.Item1 == val);
                    if (item != default) return item.Item2;

                    return ExporterUtils.ConvertInt32(sheet.Name, columnName, _enumTypeName, value.End.Row, val);
                }
            }
            
            var method = typeof(ExporterUtils).GetMethod("ConvertEnum", BindingFlags.Public | BindingFlags.Static);
            method = method.MakeGenericMethod(_enumType);

            if (_isArray)
            {
                var arr = value.Value.ToString().Split(ExporterConsts.splitInner.ToCharArray());
                var result = Array.CreateInstance(_enumType, arr.Length);
                for (var i = 0; i < arr.Length; i++)
                {
                    var v = method.Invoke(null,
                        new object[] {sheet.Name, columnName, TypeName, value.End.Row, arr[i]});
                    result.SetValue(v, i);
                }

                return result;
            }
            else
            {
                var v = method.Invoke(null,
                    new object[] {sheet.Name, columnName, TypeName, value.End.Row, value.Value.ToString()});
                return v;
            }
        }
    }
}
