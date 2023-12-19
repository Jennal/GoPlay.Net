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
        _finishList = new Dictionary<string, ConfValues>();

        if (!Directory.Exists(xlsFolder))
        {
            ExporterUtils.Error("Excel目录不存在，请检查：" + xlsFolder);
            return;
        }

        if (!Directory.Exists(outFolder))
        {
            ExporterUtils.CreateFolderIfNotExists(outFolder);
        }

        var files = Directory.EnumerateFiles(xlsFolder, "*.*")
            .Where(p => ExporterConsts.extensionPattern.Any(p.EndsWith))
            .Where(o => !Path.GetFileName(o).StartsWith("~$") && !o.EndsWith(".converting"))
            .ToList();
        try
        {
            var i = 0;
            foreach (var xls in files)
            {
                i++;
                ExporterUtils.Info($"正在导出数据 ({i} / {files.Count}) {Path.GetFileNameWithoutExtension(xls)} ...");
                if (cache.FilterExportScriptableObject(outFolder, xls, _platform))
                {
                    ExportFile(xls);
                }
                else
                {
                    ExporterUtils.Info($"\t=>　cache验证，已忽略");
                }
            }
        }
        finally
        {
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
        var types = ReflectionHelper.GetTypesInAllLoadedAssemblies(t =>
            t != typeof(TypeResolverBase) &&
            t != typeof(TypeResolverBase<>) &&
            (t.InheritsFrom(typeof(TypeResolverBase)) || t.InheritsFrom(typeof(TypeResolverBase<>))));

        foreach (var type in types)
        {
            var resolver = (TypeResolverBase) Activator.CreateInstance(type);
            _typeResolvers.Add(resolver);
        }
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

    private static void ExportFile(string xls)
    {
        if (ExporterConsts.ignorePattern.Any(o => Path.GetFileName(xls).StartsWith(o))) return;

        var tmpFileName = xls + ".converting";
        if (File.Exists(tmpFileName))
        {
            File.Delete(tmpFileName);
        }

        File.Copy(xls, tmpFileName);

        try
        {
            using (var stream = File.Open(tmpFileName, FileMode.Open, FileAccess.Read))
            {
                var excelReader = new ExcelPackage(stream);

                var ExcelWorksheetList = new List<ExcelWorksheet>();
                foreach (var table in excelReader.Workbook.Worksheets)
                {
                    var name = table.Name;
                    if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

//                            Debug.Log($"{xls} => {name}");
                    ExcelWorksheetList.Add(table);
                }

                //执行导出
                OnAllExportBegin(xls, excelReader);
                foreach (var table in ExcelWorksheetList)
                {
                    Export(xls, table, excelReader);
                }

                OnAllExportFinish(xls, excelReader);
            }
        }
        finally
        {
            File.Delete(tmpFileName);
        }
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

    private static void Export(string xls, ExcelWorksheet table, ExcelPackage package)
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
        var asset = CreateAsset();
        var outPath = Path.Combine(_outFolder, ExporterConsts.dataFolder, variantName, typeName + ".asset");

        OnExportBegin(xls, table);

        FillAsset(asset, table, package);
        SaveAsset(asset, table, outPath);

        OnExportFinish(xls, table, outPath);

        _finishList[tableName] = asset;
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