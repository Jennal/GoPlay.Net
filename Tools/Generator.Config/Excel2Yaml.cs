using System.Collections;
using DotLiquid;
using Generator.Core;
using GoPlay.Generators.Config.CustomYamlConverter;
using OfficeOpenXml;
using YamlDotNet.Serialization;

namespace GoPlay.Generators.Config;

public class Excel2Yaml
{
    public class ConfValues : List<Dictionary<string, object>> {}

    private static List<TypeResolverBase> _typeResolvers = new List<TypeResolverBase>();
    private static List<DataExportHookBase> _hooks = new List<DataExportHookBase>();
    private static ISerializer _serializer;
    private static Dictionary<string, ConfValues> _finishList;

    private static string _platform;
    private static string _xlsFolder;
    private static string _outFolder;

    private static string DATA_TEMPLETE = GeneratorUtils.GetTpl("tpl_data");
    
    public static void Generate(string xlsFolder, string outFolder, string platform)
    {
        _platform = platform;
        _xlsFolder = xlsFolder;
        _outFolder = outFolder;
        
        PrepareHooks();
        PrepareResolvers();

        var cache = ExportCache.Load(_xlsFolder, platform);
        _serializer = BuildSerializer();
        _finishList = new Dictionary<string, ConfValues>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(xlsFolder))
        {
            ExporterUtils.Error("Excel目录不存在，请检查：" + xlsFolder);
            return;
        }

        if (!Directory.Exists(outFolder))
        {
            ExporterUtils.CreateFolderIfNotExists(outFolder);
        }

        var files = ExporterUtils.GetExcelFiles(xlsFolder);
        var plan = DataExportPlan.Create(files, cache, outFolder, _platform);

        var i = 0;
        foreach (var xls in files)
        {
            i++;
            ExporterUtils.Info($"正在导出数据 ({i} / {files.Count}) {Path.GetRelativePath(xlsFolder, xls)} ...");
            if (plan.ShouldExportFile(xls))
            {
                ExportFile(xls, plan);
            }
            else
            {
                ExporterUtils.Info($"\t=>　cache验证，已忽略");
            }
        }

        //clear memory
        _finishList = null;

        cache.RefreshExportScriptableObject(_xlsFolder, platform, files);
        ExporterUtils.Info("Export Complete!");
    }

    private static ISerializer BuildSerializer()
    {
        var bigIntegerConverter = new BigIntegerConverter();
        var builder = new SerializerBuilder()
            .WithTypeConverter(bigIntegerConverter);

        bigIntegerConverter.ValueSerializer = builder.BuildValueSerializer();
        return builder.Build();
    }

    private static void PrepareResolvers()
    {
        _typeResolvers.Clear();
        _typeResolvers.AddRange(TypeResolverHelper.CreateAll());
    }

    private static void PrepareHooks()
    {
        _hooks.Clear();
        var types = ReflectionHelper.GetTypesInAllLoadedAssemblies(t =>
            t != typeof(DataExportHookBase) &&
            t.InheritsFrom(typeof(DataExportHookBase)));

        foreach (var type in types)
        {
            var resolver = (DataExportHookBase) Activator.CreateInstance(type);
            _hooks.Add(resolver);
        }
    }

    private static void ExportFile(string xls, DataExportPlan plan)
    {
        ExporterUtils.ReadExcel(xls, excelReader =>
        {
            var ExcelWorksheetList = new List<ExcelWorksheet>();
            foreach (var table in excelReader.Workbook.Worksheets)
            {
                var name = table.Name;
                if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;
                if (!plan.ShouldExportSheet(xls, name)) continue;

                ExcelWorksheetList.Add(table);
            }

            //执行导出
            OnAllExportBegin(xls, excelReader);
            foreach (var table in ExcelWorksheetList)
            {
                Export(xls, table, excelReader, plan);
            }

            OnAllExportFinish(xls, excelReader);
        });
    }

    private static void OnAllExportBegin(string xls, ExcelPackage excel)
    {
        foreach (var hook in _hooks)
        {
            hook.OnAllExportBegin(xls, excel);
        }
    }

    private static void OnAllExportFinish(string xls, ExcelPackage excel)
    {
        foreach (var hook in _hooks)
        {
            hook.OnAllExportFinish(xls, excel);
        }
    }

    private static void Export(string xls, ExcelWorksheet table, ExcelPackage package, DataExportPlan plan)
    {
        var tableName = GetTableName(table);
        if (string.IsNullOrEmpty(tableName))
        {
            ExporterUtils.Error($"[错误]表名不存在：{xls} => {table.Name}");
            return;
        }

        if (!ExporterUtils.HasValidColumns(table, _platform)) return;
        
        var mainName = ExporterUtils.GetVariantMainName(tableName);
        var variantName = ExporterUtils.GetVariantName(tableName);

        var typeName = GetTypeNameByTableName(mainName);
        var outPath = Path.Combine(_outFolder, ExporterConsts.dataFolder, variantName, typeName + ".asset");

        //多个Excel中的同名Sheet，按文件名顺序追加到同一个Asset
        var dataKey = DataExportPlan.GetDataKeyBySheetName(table.Name);
        if (!_finishList.TryGetValue(dataKey, out var asset))
        {
            asset = CreateAsset();
            _finishList[dataKey] = asset;
        }
        else
        {
            ExporterUtils.Info($"\t=>　合并到已有数据：{typeName} ({variantName})");
        }

        OnExportBegin(xls, table);

        var startIndex = asset.Count;
        FillAsset(asset, table, package);
        plan.CheckDuplicateIds(dataKey, xls, ExporterUtils.GetIdFieldName(table, _platform), asset.Skip(startIndex));
        SaveAsset(asset, table, outPath);

        OnExportFinish(xls, table, outPath);
    }

    private static void SaveAsset(ConfValues asset, ExcelWorksheet table, string outPath)
    {
        var yaml = _serializer.Serialize(asset);
        var content = GeneratorUtils.RenderTpl(DATA_TEMPLETE, new {data = yaml});
        
        ExporterUtils.CreateFileFolderIfNotExists(outPath);
        File.WriteAllText(outPath, content);
    }

    private static string GetTypeName(ExcelWorksheet table)
    {
        var tableName = GetTableName(table);
        var mainName = ExporterUtils.GetVariantMainName(tableName);
        var typeName = GetTypeNameByTableName(mainName);
        return typeName;
    }

    private static void OnExportBegin(string xls, ExcelWorksheet table)
    {
        foreach (var hook in _hooks)
        {
            if (!hook.Recognize(xls, table)) continue;

            hook.OnExportBegin(xls, table);
        }
    }

    private static void OnExportFinish(string xls, ExcelWorksheet table, object asset)
    {
        foreach (var hook in _hooks)
        {
            if (!hook.Recognize(xls, table)) continue;

            hook.OnExportFinish(xls, table, asset);
        }
    }

    private static string GetTableName(ExcelWorksheet table)
    {
        return table.Name.Substring(ExporterConsts.exportPrefix.Length);
    }

    private static string GetTypeNameByTableName(string tableName)
    {
        return tableName + ExporterConsts.confClassSuffix + "s";
    }

    private static ConfValues CreateAsset()
    {
        return new ConfValues();
    }

    private static void FillAsset(ConfValues asset, ExcelWorksheet table, ExcelPackage package)
    {
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);
        var fieldPlatforms = ExporterUtils.GetFieldPlatform(table);

        var rowColumn = ExporterUtils.GetRowColumn(table);
        for (int line = ExporterConsts.LINE_START; line <= rowColumn.y; line++)
        {
            var item = new Dictionary<string, object>();

            //忽略注释行
            if (ExporterUtils.IsCommentLine(table, line)) continue;

            //忽略空行
            if (ExporterUtils.IsEmptyLine(table, line, rowColumn.x)) continue;

            for (int i = 0; i < rowColumn.x; i++)
            {
                var platform = fieldPlatforms[i];
                var name = fieldNames[i];
                var type = fieldTypes[i];

                //校验平台 c/s : 客户端/服务端
                if (!platform.Contains(_platform)) continue;

                //忽略无类型字段
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
                {
                    continue;
                }

                var isArray = name.EndsWith("[]");
                if (isArray) name = name.Substring(0, name.Length - 2);

                var cell = table.Cells[line, i + 1];

                //忽略引用类型，第二遍遍历补足
                //!!取消对引用的支持
//                    if (type.StartsWith("ref("))
//                    {
//                        refList[name] = column;
//                        continue;
//                    }

                try
                {
                    if (isArray)
                    {
                        AppendItemToArray(table, type, name, item, cell);
                    }
                    else
                    {
                        //普通类型
                        object val = GetValue(table, type, cell, name);
                        item[name] = val;
                    }
                }
                catch (Exception err)
                {
                    throw new Exception($"{table.Name}@[{cell.Address}] => {err}");
                }
            }

            asset.Add(item);
        }
    }

    private static void AppendItemToArray(ExcelWorksheet table, string type, string name,
        Dictionary<string, object> item, ExcelRange cell)
    {
        //ignore
        if (IsIgnore(cell.GetValue<string>())) return;

        object val = GetValue(table, type, cell, name);
        var list = item.ContainsKey(name) ? (ArrayList) item[name] : new ArrayList();
        list.Add(val);
        item[name] = list;
    }

    private static object GetValue(ExcelWorksheet table, string type, ExcelRange value, string name)
    {
        foreach (var typeResolver in _typeResolvers)
        {
            if (!typeResolver.RecognizeType(type)) continue;

            if (typeResolver.IsEmpty(table, value))
            {
                return typeResolver.Default;
            }
            else
            {
                return typeResolver.GetValue(table, name, value);
            }
        }

        return null;
    }

    private static bool IsIgnore(string value)
    {
        return value == "-";
    }

    public static void Clear(string outFolder)
    {
        var folderPath = Path.Combine(outFolder, ExporterConsts.dataFolder);
        if (!Directory.Exists(folderPath)) return;
        
        Directory.Delete(folderPath, true);
    }
}