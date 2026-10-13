using System.Collections;
using System.Text;
using OfficeOpenXml;

namespace GoPlay.Generators.Config;

/// <summary>
/// 同名Sheet的表头基准：优先取Excel根目录下按文件名排序的第一个文件，根目录下没有时取全部文件排序后的第一个。
/// 其余同名Sheet的表头只能是基准表头的子集，缺少的字段按空值导出。
/// </summary>
public class SheetBaseline
{
    public class BaseField
    {
        public string Name = "";
        public string Type = "";
        public bool IsArray;

        //索引标记，如 "k,k1"，非索引字段为空
        public string Index = "";

        public bool IsSameAs(BaseField other)
        {
            return string.Equals(Type, other.Type, StringComparison.OrdinalIgnoreCase)
                   && IsArray == other.IsArray
                   && Index == other.Index;
        }

        public override string ToString()
        {
            var name = IsArray ? $"{Name}[]" : Name;
            return string.IsNullOrEmpty(Index) ? $"{name} : {Type}" : $"{name} : {Type} [{Index}]";
        }
    }

    public class BaseHeader
    {
        public List<BaseField> Fields = new List<BaseField>();
        public string? IdField;
    }

    private readonly string _platform;

    //dataKey => 基准Excel
    private readonly Dictionary<string, string> _baseXls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    //实体名（含variant，如 ItemConf@en） => 基准Excel
    private readonly Dictionary<string, string> _baseEntities = new Dictionary<string, string>();

    //dataKey => 基准表头
    private readonly Dictionary<string, BaseHeader> _headers = new Dictionary<string, BaseHeader>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loadedFiles = new HashSet<string>();

    private SheetBaseline(string platform)
    {
        _platform = platform;
    }

    /// <param name="files">ExporterUtils.GetExcelFiles 的结果（已按文件名排序）</param>
    public static SheetBaseline Create(List<string> files, string xlsFolder, string platform)
    {
        var baseline = new SheetBaseline(platform);
        var rootFolder = NormalizeFolder(xlsFolder);
        var rootFiles = files.Where(o => NormalizeFolder(Path.GetDirectoryName(o) ?? "") == rootFolder).ToList();
        var ordered = rootFiles.Concat(files.Except(rootFiles));

        foreach (var xls in ordered)
        {
            foreach (var sheetName in ExporterUtils.GetSheetNames(xls))
            {
                if (!sheetName.StartsWith(ExporterConsts.exportPrefix)) continue;

                if (string.IsNullOrEmpty(ExporterUtils.GetVariantMainName(sheetName))) continue;

                var dataKey = DataExportPlan.GetDataKeyBySheetName(sheetName);
                if (baseline._baseXls.TryAdd(dataKey, xls))
                {
                    baseline._baseEntities[EntityNameBySheetName(sheetName)] = xls;
                }
            }
        }

        return baseline;
    }

    /// <summary>
    /// 与 ExporterUtils.EntityNameFromTable(table, true) 规则一致，用于匹配缓存中的实体名
    /// </summary>
    private static string EntityNameBySheetName(string sheetName)
    {
        var entityName = ExporterUtils.ToPascalCase(ExporterUtils.GetVariantMainName(sheetName)) + ExporterConsts.confClassSuffix;
        var variantName = ExporterUtils.GetVariantName(sheetName);
        if (!string.IsNullOrEmpty(variantName)) entityName += $"@{variantName}";
        return entityName;
    }

    private static string NormalizeFolder(string folder)
    {
        return Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public bool IsBase(string xls, string dataKey)
    {
        return _baseXls.TryGetValue(dataKey, out var baseXls) && baseXls == xls;
    }

    public bool IsBaseEntity(string xls, string entityName)
    {
        return _baseEntities.TryGetValue(entityName, out var baseXls) && baseXls == xls;
    }

    public string? GetBaseXls(string dataKey)
    {
        return _baseXls.TryGetValue(dataKey, out var baseXls) ? baseXls : null;
    }

    public BaseHeader? GetBaseHeader(string dataKey)
    {
        if (_headers.TryGetValue(dataKey, out var header)) return header;

        var baseXls = GetBaseXls(dataKey);
        if (baseXls == null || !_loadedFiles.Add(baseXls)) return null;

        ExporterUtils.ReadExcel(baseXls, package =>
        {
            foreach (var table in package.Workbook.Worksheets)
            {
                var name = table.Name;
                if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

                var key = DataExportPlan.GetDataKeyBySheetName(name);
                if (!IsBase(baseXls, key)) continue;

                _headers[key] = ParseHeader(table);
            }
        });

        return _headers.TryGetValue(dataKey, out header) ? header : null;
    }

    private BaseHeader ParseHeader(ExcelWorksheet table)
    {
        return new BaseHeader
        {
            IdField = ExporterUtils.GetIdFieldName(table, _platform),
            Fields = ParseFields(table),
        };
    }

    private List<BaseField> ParseFields(ExcelWorksheet table)
    {
        var fields = new List<BaseField>();
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);
        var fieldPlatforms = ExporterUtils.GetFieldPlatform(table);
        var added = new HashSet<string>();
        for (var i = 0; i < fieldNames.Count; i++)
        {
            var name = fieldNames[i];
            var type = fieldTypes[i];
            var platform = fieldPlatforms[i];
            if (!platform.Contains(_platform)) continue;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type)) continue;

            var isArray = name.EndsWith("[]");
            if (isArray) name = name.Substring(0, name.Length - 2);
            if (!added.Add(name)) continue;

            fields.Add(new BaseField
            {
                Name = name,
                Type = type,
                IsArray = isArray,
                Index = string.Join(",", ConfigIndex.ParseGroups(platform).Select(ConfigIndex.GroupLabel)),
            });
        }

        return fields;
    }

    /// <summary>
    /// 导出数据前检查本次要导出的非基准Sheet：不能有基准表头中不存在（或类型、索引标记不同）的字段，也不能缺少索引字段
    /// </summary>
    public bool CheckSheets(List<string> files, DataExportPlan plan)
    {
        var errors = new List<string>();
        foreach (var xls in files)
        {
            if (!plan.ShouldExportFile(xls)) continue;

            var sheetNames = ExporterUtils.GetSheetNames(xls)
                .Where(o => o.StartsWith(ExporterConsts.exportPrefix) && plan.ShouldExportSheet(xls, o))
                .Where(o => !IsBase(xls, DataExportPlan.GetDataKeyBySheetName(o)))
                .ToList();

            //基准表头来自其他Excel，需要在读取当前Excel之前加载
            var headers = new Dictionary<string, BaseHeader>();
            foreach (var sheetName in sheetNames)
            {
                var header = GetBaseHeader(DataExportPlan.GetDataKeyBySheetName(sheetName));
                if (header != null) headers[sheetName] = header;
            }

            if (headers.Count == 0) continue;

            ExporterUtils.ReadExcel(xls, package =>
            {
                foreach (var pair in headers)
                {
                    var table = package.Workbook.Worksheets[pair.Key];
                    if (table == null || !ExporterUtils.HasValidColumns(table, _platform)) continue;

                    var error = DiffFields(pair.Value.Fields, ParseFields(table));
                    if (error == null) continue;

                    var baseXls = GetBaseXls(DataExportPlan.GetDataKeyBySheetName(pair.Key));
                    errors.Add($"同名Sheet的表头与基准不一致（平台：{_platform}）：" +
                               $"\n\t基准：{baseXls} => {pair.Key}" +
                               $"\n\t当前：{xls} => {pair.Key}{error}");
                }
            });
        }

        foreach (var error in errors)
        {
            ExporterUtils.Error(error);
        }

        return errors.Count == 0;
    }

    private static string? DiffFields(List<BaseField> baseFields, List<BaseField> fields)
    {
        var baseDict = baseFields.ToDictionary(o => o.Name);
        var names = new HashSet<string>(fields.Select(o => o.Name));

        var extra = fields
            .Where(o => !baseDict.ContainsKey(o.Name))
            .Select(o => o.ToString())
            .ToList();
        var different = fields
            .Where(o => baseDict.TryGetValue(o.Name, out var b) && !b.IsSameAs(o))
            .Select(o => $"{o}（基准为 {baseDict[o.Name]}）")
            .ToList();
        var missingIndex = baseFields
            .Where(o => !string.IsNullOrEmpty(o.Index) && !names.Contains(o.Name))
            .Select(o => o.ToString())
            .ToList();
        if (extra.Count == 0 && different.Count == 0 && missingIndex.Count == 0) return null;

        var sb = new StringBuilder();
        if (extra.Count > 0) sb.Append($"\n\t基准中不存在：{string.Join(", ", extra)}");
        if (different.Count > 0) sb.Append($"\n\t与基准不一致：{string.Join(", ", different)}");
        if (missingIndex.Count > 0) sb.Append($"\n\t缺少索引字段：{string.Join(", ", missingIndex)}");
        return sb.ToString();
    }

    /// <summary>
    /// 为非基准Sheet导出的数据补齐基准表头中缺少的字段：普通字段取类型默认值，数组字段为空数组
    /// </summary>
    public void FillMissingFields(string dataKey, IEnumerable<Dictionary<string, object>> items, List<TypeResolverBase> resolvers)
    {
        var header = GetBaseHeader(dataKey);
        if (header == null) return;

        foreach (var item in items)
        {
            foreach (var field in header.Fields)
            {
                if (item.ContainsKey(field.Name)) continue;

                item[field.Name] = field.IsArray
                    ? new ArrayList()
                    : resolvers.FirstOrDefault(o => o.RecognizeType(field.Type))?.Default!;
            }
        }
    }
}
