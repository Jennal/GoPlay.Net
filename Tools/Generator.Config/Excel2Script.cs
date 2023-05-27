using System.Text;
using DotLiquid;
using Generator.Core;
using OfficeOpenXml;

namespace GoPlay.Generators.Config;

public class Excel2Script
{
    static List<TypeResolverBase> _typeResolvers = new List<TypeResolverBase>();
    private static HashSet<string> _finishedTypeNames = new HashSet<string>();
    private static List<ScriptGenHookBase> _hooks = new List<ScriptGenHookBase>();

    private static string[] BASIC_TYPES => GeneratorUtils.GetBasicConf("basic_types");
    private static string[] BASIC_NAMESPACES = GeneratorUtils.GetBasicConf("basic_ns_conf");

    private static string CLASS_TEMPLETE_FULL = GeneratorUtils.GetTpl("tpl_class_conf");
    private static string CLASS_MANAGER_TEMPLETE = GeneratorUtils.GetTpl("tpl_class_manager");

    public static void Generate(string xlsFolder, string csFolder, string platform, string templateConfPath="", string templateManagerPath="")
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        var tplConf = string.IsNullOrEmpty(templateConfPath) ? CLASS_TEMPLETE_FULL : File.ReadAllText(templateConfPath);
        var tplManager = string.IsNullOrEmpty(templateManagerPath) ? CLASS_MANAGER_TEMPLETE : File.ReadAllText(templateManagerPath);
        
        PrepareResolvers();
        PrepareHooks();

        var cache = ExportCache.Load(xlsFolder, platform);
        _finishedTypeNames = new HashSet<string>();

        if (!Directory.Exists(xlsFolder))
        {
            ExporterUtils.Error($"Excel目录不存在：{xlsFolder}");
            return;
        }

        if (!CheckConflictNames(xlsFolder)) return;

        var files = Directory.EnumerateFiles(xlsFolder, "*.*")
            .Where(p => ExporterConsts.extensionPattern.Any(p.EndsWith))
            .Where(xls => !xls.EndsWith(".converting") &&
                          !ExporterConsts.ignorePattern.Any(o => Path.GetFileName(xls).StartsWith(o)))
            .ToList();
        for (var i = 0; i < files.Count; i++)
        {
            var xls = files[i];
            if (!cache.FilterExportCSharp(csFolder, xls, platform))
            {
                foreach (var entity in cache.GetSheetEntities(xls))
                {
                    if (entity.Platform.Contains(platform))
                    {
                        var typeName = ExporterUtils.GetVariantMainName(entity.Name);
                        _finishedTypeNames.Add(typeName);
                    }

                    ExporterUtils.Info($"\t=>　cache验证，已忽略: {Path.GetFileNameWithoutExtension(xls)} => {entity.Name}");
                }

                continue;
            }

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
                    foreach (var sheet in excelReader.Workbook.Worksheets)
                    {
                        var name = sheet.Name;
                        if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

//                            Debug.Log($"{xls} => {name}");
                        ExporterUtils.Info(
                            $"正在导出代码 ({i+1} / {files.Count}) {Path.GetFileNameWithoutExtension(xls)} => {name.Substring(ExporterConsts.exportPrefix.Length)} ...");
                        ConvertToClasses(xls, sheet, platform, csFolder, tplConf);
                    }
                }
            }
            finally
            {
                File.Delete(tmpFileName);
            }
        }

        CreateManagerCode(csFolder, tplManager);
        HookAllFinish();
        cache.RefreshExportCSharp(xlsFolder, platform, files);

        _finishedTypeNames = null;
    }

    static void ConvertToClasses(string xls, ExcelWorksheet table, string platform, string csFolder, string tpl)
    {
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        var entityName = ExporterUtils.EntityNameFromTable(table);
        if (_finishedTypeNames.Contains(entityName)) return;

        if (string.IsNullOrEmpty(tableName))
        {
            ExporterUtils.Error($"[错误]表名不存在：{xls} => {table.Name}");
            return;
        }

        if (!ExporterUtils.HasValidColumns(table, platform)) return;

        var rowColumns = ExporterUtils.GetRowColumn(table);
        ConvertTable(xls, table, rowColumns, platform, csFolder, tpl);
    }

    static void ConvertTable(string xls, ExcelWorksheet table, Vector2Int rowColumn, string exportPlatform, string csFolder, string tpl)
    {
        HookBegin(xls, table, rowColumn);

        var tplData = new TemplateData
        {
            namespaces = new List<string>(),
            fields = new List<TemplateField>(),
        };
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        if (rowColumn.x <= 0 || rowColumn.y <= 0) return;

        var tableDesc = FixMultilineComment(table.Cells[ExporterConsts.LINE_TABLE_DESC, 1].GetValue<string>());
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);
        var fieldDescs = ExporterUtils.GetFieldDescs(table);
        var fieldPlatforms = ExporterUtils.GetFieldPlatform(table);
        var entityName = ExporterUtils.EntityNameFromTable(table);

        //template
        tplData.excelFile = xls;
        tplData.tableName = tableName;
        tplData.tableDesc = tableDesc.Split("\n");
        tplData.entityName = entityName;
        
        //name => exists
        var arrDict = new Dictionary<string, bool>();

        var namespaces = tplData.namespaces;
        namespaces.AddRange(BASIC_NAMESPACES);
        for (var i = 0; i < rowColumn.x; i++)
        {
            var fieldData = new TemplateField();
            var platform = fieldPlatforms[i];
            var name = fieldNames[i];
            var type = fieldTypes[i];
            var desc = fieldDescs[i];
            var isArray = false;

            //校验平台 c/s : 客户端/服务端
            if (!platform.Contains(exportPlatform)) continue;

            //忽略为空的字段
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type)) continue;

            //Type Resolver
            var resolver = GetResolver(xls, table, type);

            //重设类型
            type = resolver.TypeName;

            //自动引用名称空间
            var typeNs = resolver.Namespace;
            if (!string.IsNullOrEmpty(typeNs))
            {
                if (!BASIC_TYPES.Contains(type) && !BASIC_NAMESPACES.Contains(typeNs))
                {
                    if (!namespaces.Contains(typeNs)) namespaces.Add(typeNs);
                    ExporterUtils.Info($"--------------------> {type} => {typeNs}");
                }
            }
            else
            {
                var csType = ReflectionHelper.GetTypeInAllLoadedAssemblies(type);
                if (!BASIC_TYPES.Contains(type))
                {
                    if (csType != null)
                    {
                        if (!string.IsNullOrEmpty(csType.Namespace) && !BASIC_NAMESPACES.Contains(csType.Namespace))
                        {
                            var ns = csType.Namespace;
                            if (!namespaces.Contains(ns)) namespaces.Add(ns);
                            ExporterUtils.Info($"--------------------> {type} => {csType.Namespace}");
                        }
                    }
                }
            }
            
            var fieldName = ExporterUtils.ToCamelCase(name);
            string fieldType;
            if (!GetFieldType(type, resolver.Type, out fieldType))
            {
                ExporterUtils.Error("[错误]存在错误字段类型：" + type + " - " + tableName + "." + name);
                return;
            }

            if (fieldName.EndsWith("[]"))
            {
                if (arrDict.ContainsKey(fieldName)) continue;

                arrDict[fieldName] = true;
                fieldName = fieldName.Substring(0, fieldName.Length - 2);
                fieldType = $"{fieldType}[]";

                isArray = true;
            }

            fieldData.typeName = fieldType;
            fieldData.name = fieldName;
            fieldData.desc = desc.Split("\n");
            fieldData.isArray = isArray;
            
            tplData.fields.Add(fieldData);
        }

        var content = GeneratorUtils.RenderTpl(tpl, new {data = tplData});
        var path = WriteEntityFile(csFolder, entityName, content);
        _finishedTypeNames.Add(entityName);

        HookFinish(xls, table, rowColumn, path);
    }

    private static void HookFinish(string xls, ExcelWorksheet table, Vector2Int rowColumn, string path)
    {
        foreach (var hook in _hooks)
        {
            if (!hook.Recognize(xls, table, rowColumn)) continue;
            hook.OnExportFinish(xls, table, rowColumn, path);
        }
    }

    private static void HookBegin(string xls, ExcelWorksheet table, Vector2Int rowColumn)
    {
        foreach (var hook in _hooks)
        {
            if (!hook.Recognize(xls, table, rowColumn)) continue;
            hook.OnExportBegin(xls, table, rowColumn);
        }
    }

    private static TypeResolverBase GetResolver(string xls, ExcelWorksheet table, string typeName)
    {
        foreach (var resolver in _typeResolvers)
        {
            if (resolver.RecognizeType(typeName)) return resolver;
        }

        throw new Exception($"无法识别类型：{typeName}\n\n{xls}\n{table.Name}");
    }

    private static bool GetFieldType(string type, Type csType, out string fieldType)
    {
        //引用类型: ref(TypeName,FieldName)
        if (type.StartsWith("ref("))
        {
            fieldType = type.Substring(4, type.IndexOf(",") - 4);
            fieldType = ExporterUtils.FixRefType(fieldType);
            return true;
        }

        //普通类型
//            return TYPE_MAP.TryGetValue(type, out fieldType);
        if (csType == null)
        {
            fieldType = type;
        }
        else
        {
            fieldType = csType.FullName
                .Replace(csType.Namespace + ".", "")
                .Replace("+", ".");

            //类型会被转成普通类型传进来，
            if (type.EndsWith("[]") && !fieldType.EndsWith("[]")) fieldType += "[]";
        }

        return true;
    }

    static string WriteEntityFile(string csFolder, string entityName, string content)
    {
        ExporterUtils.Log("写入文件：" + entityName);
        var path = Path.Combine(csFolder, ExporterConsts.csFolder, entityName + "s.cs");
        ExporterUtils.CreateFileFolderIfNotExists(path);
        File.Delete(path);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
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
            t != typeof(ScriptGenHookBase) &&
            t.InheritsFrom(typeof(ScriptGenHookBase)));

        foreach (var type in types)
        {
            var resolver = (ScriptGenHookBase) Activator.CreateInstance(type);
            _hooks.Add(resolver);
        }
    }

    private static void HookAllFinish()
    {
        foreach (var hook in _hooks)
        {
            hook.OnExportAllFinished();
        }
    }

    private static bool CheckConflictNames(string xlsFolder)
    {
        var set = new Dictionary<string, string>();

        foreach (var xls in Directory.EnumerateFiles(xlsFolder, "*.xlsx"))
        {
            if (ExporterConsts.ignorePattern.Any(o => Path.GetFileName(xls).StartsWith(o))) continue;

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
                    foreach (var sheet in excelReader.Workbook.Worksheets)
                    {
                        var name = sheet.Name;
                        if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

                        if (set.ContainsKey(name))
                        {
                            var file = set[name];
                            ExporterUtils.Error($"{name} exists in {file} and {xls}");
                            return false;
                        }

                        set[name] = xls;
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                File.Delete(tmpFileName);
            }
        }

        return true;
    }

    private static void CreateManagerCode(string csFolder, string tpl)
    {
        var fields = "";

        var tplData = new TemplateData
        {
            fields = new List<TemplateField>(),
        };
        foreach (var typeName in _finishedTypeNames)
        {
            var privateName = ExporterUtils.ToPrivateName(typeName);
            tplData.fields.Add(new TemplateField
            {
                typeName = typeName,
                privateName = privateName
            });
        }

        var code = GeneratorUtils.RenderTpl(tpl, new {data = tplData});
        WriteManagerFile(csFolder, code);
    }

    static void WriteManagerFile(string csFolder, string content)
    {
        var path = Path.Combine(csFolder, ExporterConsts.mgrFile);
        var fileName = Path.GetFileNameWithoutExtension(path);
        ExporterUtils.Log("写入文件：" + fileName);
        ExporterUtils.CreateFileFolderIfNotExists(path);
        File.Delete(path);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    static string FixMultilineComment(string comment)
    {
        if (string.IsNullOrEmpty(comment)) return string.Empty;
            
        var arr = comment.Split("\n".ToCharArray());
        for (var i = 1; i < arr.Length; i++)
        {
            arr[i] = "	/// " + arr[i];
        }

        return string.Join("\r\n", arr);
    }

    public static void Clear(string csFolder)
    {
        var folderPath = Path.Combine(csFolder, ExporterConsts.csFolder);
        if (!Directory.Exists(folderPath)) return;
        
        Directory.Delete(folderPath, true);
    }
}