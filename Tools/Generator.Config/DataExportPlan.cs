namespace GoPlay.Generators.Config;

/// <summary>
/// 数据导出计划：多个Excel中同名的Sheet会合并导出到同一个数据文件。
/// 只要一个数据文件的任意来源Excel发生变化（或被删除），该数据文件的所有来源Sheet都需要重新导出。
/// </summary>
public class DataExportPlan
{
    public List<string> Files { get; private set; } = new List<string>();

    private readonly Dictionary<string, List<string>> _sheetsToExport = new Dictionary<string, List<string>>();

    public static DataExportPlan Create(List<string> files, ExportCache cache, string outFolder, string platform)
    {
        var plan = new DataExportPlan { Files = files };

        var sheetsOfFile = new Dictionary<string, List<string>>();
        foreach (var xls in files)
        {
            sheetsOfFile[xls] = ExporterUtils.GetSheetNames(xls)
                .Where(o => o.StartsWith(ExporterConsts.exportPrefix))
                .ToList();
        }

        var dirtyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var xls in files)
        {
            if (!cache.FilterExportScriptableObject(outFolder, xls, platform)) continue;

            foreach (var sheetName in sheetsOfFile[xls])
            {
                dirtyKeys.Add(GetDataKeyBySheetName(sheetName));
            }
        }

        if (cache.Dict != null)
        {
            var fileSet = new HashSet<string>(files);
            foreach (var pair in cache.Dict.Where(o => !fileSet.Contains(o.Key)))
            {
                foreach (var entity in pair.Value.SheetEntities)
                {
                    if (!entity.Platform.Contains(platform)) continue;
                    dirtyKeys.Add(GetDataKeyByEntityName(entity.Name));
                }
            }
        }

        foreach (var xls in files)
        {
            plan._sheetsToExport[xls] = sheetsOfFile[xls]
                .Where(o => dirtyKeys.Contains(GetDataKeyBySheetName(o)))
                .ToList();
        }

        return plan;
    }

    //dataKey => (id => 首次出现的Excel)
    private readonly Dictionary<string, Dictionary<string, string>> _idSources =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 合并导出时，检查同一个数据文件中来自不同Excel的ID是否重复，仅警告
    /// </summary>
    public void CheckDuplicateIds(string dataKey, string xls, string? idField, IEnumerable<Dictionary<string, object>> items)
    {
        if (string.IsNullOrEmpty(idField)) return;

        if (!_idSources.TryGetValue(dataKey, out var sources))
        {
            sources = new Dictionary<string, string>();
            _idSources[dataKey] = sources;
        }

        foreach (var item in items)
        {
            if (!item.TryGetValue(idField, out var val)) continue;

            var id = val?.ToString();
            if (string.IsNullOrEmpty(id)) continue;

            if (!sources.TryGetValue(id, out var firstXls))
            {
                sources[id] = xls;
                continue;
            }

            if (firstXls == xls) continue;

            ExporterUtils.Warning($"跨文件ID重复：{dataKey} 中 {idField}={id}" +
                                  $"\n\t{firstXls}\n\t{xls}");
        }
    }

    public bool ShouldExportFile(string xls)
    {
        return _sheetsToExport.TryGetValue(xls, out var sheets) && sheets.Count > 0;
    }

    public bool ShouldExportSheet(string xls, string sheetName)
    {
        return _sheetsToExport.TryGetValue(xls, out var sheets) && sheets.Contains(sheetName);
    }

    /// <summary>
    /// 数据文件的唯一标识：{variant}/{TypeName}，与导出路径一一对应
    /// </summary>
    public static string GetDataKeyBySheetName(string sheetName)
    {
        var tableName = sheetName.StartsWith(ExporterConsts.exportPrefix)
            ? sheetName.Substring(ExporterConsts.exportPrefix.Length)
            : sheetName;
        var mainName = ExporterUtils.GetVariantMainName(tableName);
        var variantName = ExporterUtils.GetVariantName(tableName);
        return $"{variantName}/{mainName}{ExporterConsts.confClassSuffix}s";
    }

    /// <summary>
    /// 缓存中的实体名（如 ItemConf@en）对应的数据文件标识，规则与 ExportCache 保持一致
    /// </summary>
    private static string GetDataKeyByEntityName(string entityName)
    {
        var mainName = ExporterUtils.GetVariantMainName(entityName);
        var variantName = ExporterUtils.GetVariantName(entityName);
        return $"{variantName}/{mainName}s";
    }
}
