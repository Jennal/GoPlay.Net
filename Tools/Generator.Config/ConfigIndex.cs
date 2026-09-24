using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace GoPlay.Generators.Config;

/// <summary>
/// platform 行中的索引标记：k / k1 / k2 ...（k 等价于 k0）。
/// 同一分组的列按列顺序组成一个联合唯一索引，一列可以同时属于多个分组，例如：skk1
/// </summary>
public static class ConfigIndex
{
    private static readonly Regex MarkerRegex = new Regex(@"k(\d*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static List<int> ParseGroups(string platform)
    {
        var groups = new SortedSet<int>();
        if (string.IsNullOrEmpty(platform)) return groups.ToList();

        foreach (Match match in MarkerRegex.Matches(platform))
        {
            var text = match.Groups[1].Value;
            groups.Add(text.Length == 0 ? 0 : int.Parse(text));
        }

        return groups.ToList();
    }

    public static string GroupLabel(int group)
    {
        return group == 0 ? "k" : $"k{group}";
    }

    public static bool IsArray(string name, string type)
    {
        return name.EndsWith("[]") || type.EndsWith("[]");
    }

    /// <summary>
    /// 收集当前平台导出的索引列：分组 => 列序号（从0开始，按列顺序）。数组列不能作为索引，出错时返回null
    /// </summary>
    public static SortedDictionary<int, List<int>>? CollectColumns(ExcelWorksheet table, string exportPlatform, out string? error)
    {
        error = null;
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);
        var fieldPlatforms = ExporterUtils.GetFieldPlatform(table);

        var result = new SortedDictionary<int, List<int>>();
        for (var i = 0; i < fieldPlatforms.Count; i++)
        {
            var platform = fieldPlatforms[i];
            var name = fieldNames[i];
            var type = fieldTypes[i];
            if (!platform.Contains(exportPlatform)) continue;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type)) continue;

            foreach (var group in ParseGroups(platform))
            {
                if (IsArray(name, type))
                {
                    error = $"[错误]索引组 {GroupLabel(group)} 的字段 {name} 是数组，数组不能作为索引：{table.Name}";
                    return null;
                }

                if (!result.TryGetValue(group, out var columns))
                {
                    columns = new List<int>();
                    result.Add(group, columns);
                }

                columns.Add(i);
            }
        }

        return result;
    }

    /// <summary>
    /// 数据导出前检查索引的唯一性。同名Sheet合并到同一个数据文件，因此按数据文件（含variant）统计。
    /// 只检查本次需要导出的Sheet，导出计划保证了同一个数据文件的所有来源Sheet会一起导出。
    /// </summary>
    public static bool CheckUnique(List<string> files, string exportPlatform, DataExportPlan plan,
        Func<ExcelWorksheet, string, ExcelRange, string, object?> getValue)
    {
        //dataKey + 索引定义 => (索引值 => 首次出现的位置)
        var sources = new Dictionary<string, Dictionary<IndexKey, string>>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var xls in files)
        {
            if (!plan.ShouldExportFile(xls)) continue;

            ExporterUtils.ReadExcel(xls, package =>
            {
                foreach (var table in package.Workbook.Worksheets)
                {
                    var name = table.Name;
                    if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;
                    if (!plan.ShouldExportSheet(xls, name)) continue;
                    if (!ExporterUtils.HasValidColumns(table, exportPlatform)) continue;

                    CheckSheet(xls, table, exportPlatform, getValue, sources, errors);
                }
            });
        }

        foreach (var error in errors)
        {
            ExporterUtils.Error(error);
        }

        return errors.Count == 0;
    }

    private static void CheckSheet(string xls, ExcelWorksheet table, string exportPlatform,
        Func<ExcelWorksheet, string, ExcelRange, string, object?> getValue,
        Dictionary<string, Dictionary<IndexKey, string>> sources, List<string> errors)
    {
        var groups = CollectColumns(table, exportPlatform, out var error);
        if (groups == null)
        {
            errors.Add($"{error}\n\t{xls}");
            return;
        }

        if (groups.Count == 0) return;

        var dataKey = DataExportPlan.GetDataKeyBySheetName(table.Name);
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);

        var indexes = groups.Select(pair =>
        {
            var names = pair.Value.Select(i => fieldNames[i]).ToArray();
            var indexName = $"{dataKey} {GroupLabel(pair.Key)}({string.Join(", ", names)})";
            if (!sources.TryGetValue(indexName, out var locations))
            {
                locations = new Dictionary<IndexKey, string>();
                sources.Add(indexName, locations);
            }

            return (indexName, columns: pair.Value, locations);
        }).ToList();

        var rowColumn = ExporterUtils.GetRowColumn(table);
        for (var line = ExporterConsts.LINE_START; line <= rowColumn.y; line++)
        {
            if (ExporterUtils.IsCommentLine(table, line)) continue;
            if (ExporterUtils.IsEmptyLine(table, line, rowColumn.x)) continue;

            var location = $"{xls} => {table.Name} 第{line}行";
            foreach (var index in indexes)
            {
                var values = new object?[index.columns.Count];
                for (var i = 0; i < values.Length; i++)
                {
                    var column = index.columns[i];
                    var cell = table.Cells[line, column + 1];
                    values[i] = getValue(table, fieldTypes[column], cell, fieldNames[column]);
                }

                //与运行时一致：单字段索引值为null时不入索引
                if (values.Length == 1 && values[0] == null) continue;

                var key = new IndexKey(values);
                if (index.locations.TryGetValue(key, out var first))
                {
                    errors.Add($"[错误]索引重复：{index.indexName} = [{key}]\n\t{first}\n\t{location}");
                    continue;
                }

                index.locations.Add(key, location);
            }
        }
    }

    private sealed class IndexKey : IEquatable<IndexKey>
    {
        private readonly object?[] _values;
        private readonly int _hashCode;

        public IndexKey(object?[] values)
        {
            _values = values;
            var hash = 17;
            foreach (var value in values)
            {
                hash = hash * 31 + (value?.GetHashCode() ?? 0);
            }

            _hashCode = hash;
        }

        public bool Equals(IndexKey? other)
        {
            if (other == null || other._values.Length != _values.Length) return false;

            for (var i = 0; i < _values.Length; i++)
            {
                if (!Equals(_values[i], other._values[i])) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as IndexKey);

        public override int GetHashCode() => _hashCode;

        public override string ToString() => string.Join(", ", _values.Select(o => o ?? "null"));
    }
}
