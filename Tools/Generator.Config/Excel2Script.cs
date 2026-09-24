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

    public static bool Generate(string xlsFolder, string csFolder, string platform, string templateConfPath="", string templateManagerPath="")
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
            return false;
        }

        var files = ExporterUtils.GetExcelFiles(xlsFolder);
        if (!CheckSameNameSheets(files, platform)) return false;

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

                    ExporterUtils.Info($"\t=>　cache验证，已忽略: {Path.GetRelativePath(xlsFolder, xls)} => {entity.Name}");
                }

                continue;
            }

            var index = i;
            ExporterUtils.ReadExcel(xls, excelReader =>
            {
                foreach (var sheet in excelReader.Workbook.Worksheets)
                {
                    var name = sheet.Name;
                    if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

                    ExporterUtils.Info(
                        $"正在导出代码 ({index + 1} / {files.Count}) {Path.GetRelativePath(xlsFolder, xls)} => {name.Substring(ExporterConsts.exportPrefix.Length)} ...");
                    ConvertToClasses(xls, sheet, platform, csFolder, tplConf);
                }
            });
        }

        CreateManagerCode(csFolder, tplManager);
        HookAllFinish();
        cache.RefreshExportCSharp(xlsFolder, platform, files);

        _finishedTypeNames = null;
        return true;
    }

    static void ConvertToClasses(string xls, ExcelWorksheet table, string platform, string csFolder, string tpl)
    {
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        var entityName = ExporterUtils.EntityNameFromTable(table);
        if (_finishedTypeNames.Contains(entityName))
        {
            ExporterUtils.Info($"\t=>　{entityName} 已生成，跳过");
            return;
        }

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
        var entityName = ExporterUtils.EntityNameFromTable(table);

        //template
        tplData.excelFile = xls;
        tplData.tableName = tableName;
        tplData.tableDesc = tableDesc.Split("\n");
        tplData.entityName = entityName;

        tplData.namespaces.AddRange(BASIC_NAMESPACES);
        var fields = BuildFields(xls, table, rowColumn.x, exportPlatform, tplData.namespaces, null, tplData.indexes);
        if (fields == null) return;
        tplData.fields.AddRange(fields);

        var content = GeneratorUtils.RenderTpl(tpl, new {data = tplData});
        var path = WriteEntityFile(csFolder, entityName, content);
        _finishedTypeNames.Add(entityName);

        HookFinish(xls, table, rowColumn, path);
    }

    /// <summary>
    /// 根据表头生成字段列表，出错时返回null
    /// </summary>
    /// <param name="namespaces">需要自动引用的名称空间，为null时不收集</param>
    /// <param name="signature">表头签名（Excel字段名 + 生成的C#类型 + 索引组），用于同名Sheet的一致性校验，为null时不收集</param>
    /// <param name="indexes">platform 行中 k / k1 / k2 ... 标记的索引，为null时不收集</param>
    static List<TemplateField>? BuildFields(string xls, ExcelWorksheet table, int columnCount, string exportPlatform,
        List<string>? namespaces, List<string>? signature, List<TemplateIndex>? indexes = null)
    {
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        var fieldNames = ExporterUtils.GetFieldNames(table);
        var fieldTypes = ExporterUtils.GetFieldTypes(table);
        var fieldDescs = ExporterUtils.GetFieldDescs(table);
        var fieldPlatforms = ExporterUtils.GetFieldPlatform(table);

        var fields = new List<TemplateField>();

        //数组字段可以配置多列同名的 xxx[]，只按第一列生成代码：fieldName => fieldType
        var arrDict = new Dictionary<string, string>();

        //索引组 => 字段（按列顺序）
        var indexFields = new SortedDictionary<int, List<TemplateField>>();

        for (var i = 0; i < columnCount; i++)
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
            if (namespaces != null)
            {
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
            }

            var fieldName = ExporterUtils.ToCamelCase(name);
            string fieldType;
            if (!GetFieldType(type, resolver.Type, out fieldType))
            {
                ExporterUtils.Error("[错误]存在错误字段类型：" + type + " - " + tableName + "." + name);
                return null;
            }

            var groups = ConfigIndex.ParseGroups(platform);
            if (groups.Count > 0 && ConfigIndex.IsArray(name, fieldType))
            {
                ExporterUtils.Error($"[错误]索引组 {ConfigIndex.GroupLabel(groups[0])} 的字段 {name} 是数组，数组不能作为索引：{xls} => {table.Name}");
                return null;
            }

            if (fieldName.EndsWith("[]"))
            {
                if (arrDict.TryGetValue(fieldName, out var firstType))
                {
                    if (firstType != fieldType)
                    {
                        ExporterUtils.Warning($"数组字段各列类型不一致，代码按第一列类型 {firstType} 生成：{xls} => {table.Name}.{name} : {fieldType}");
                    }
                    continue;
                }

                arrDict[fieldName] = fieldType;
                fieldName = fieldName.Substring(0, fieldName.Length - 2);
                fieldType = $"{fieldType}[]";

                isArray = true;
            }

            fieldData.typeName = fieldType;
            fieldData.name = fieldName;
            fieldData.desc = desc.Split("\n");
            fieldData.isArray = isArray;

            fields.Add(fieldData);

            foreach (var group in groups)
            {
                if (!indexFields.TryGetValue(group, out var list))
                {
                    list = new List<TemplateField>();
                    indexFields.Add(group, list);
                }

                list.Add(fieldData);
            }

            var groupLabels = groups.Count > 0 ? $" [{string.Join(",", groups.Select(ConfigIndex.GroupLabel))}]" : "";
            signature?.Add($"{name} : {fieldType}{groupLabels}");
        }

        var builtIndexes = BuildIndexes(xls, table, indexFields);
        if (builtIndexes == null) return null;
        indexes?.AddRange(builtIndexes);

        return fields;
    }

    private static List<TemplateIndex>? BuildIndexes(string xls, ExcelWorksheet table, SortedDictionary<int, List<TemplateField>> indexFields)
    {
        var result = new List<TemplateIndex>();
        foreach (var pair in indexFields)
        {
            var label = ConfigIndex.GroupLabel(pair.Key);
            var fields = pair.Value;
            var name = string.Join("And", fields.Select(o => char.ToUpperInvariant(o.name[0]) + o.name.Substring(1)));

            var exists = result.FirstOrDefault(o => o.name == name);
            if (exists != null)
            {
                ExporterUtils.Error($"[错误]索引组 {exists.label} 和 {label} 的字段完全相同（TryGetBy{name}），请删除其中一个：{xls} => {table.Name}");
                return null;
            }

            var paramNames = fields.Select(o => o.name).ToList();
            result.Add(new TemplateIndex
            {
                label = label,
                name = name,
                keyType = fields.Count == 1 ? fields[0].typeName : $"({string.Join(", ", fields.Select(o => o.typeName))})",
                parameters = string.Join(", ", fields.Select(o => $"{o.typeName} {o.name}")),
                parameterKey = fields.Count == 1 ? fields[0].name : $"({string.Join(", ", paramNames)})",
                valueKey = fields.Count == 1 ? $"conf.{fields[0].name}" : $"({string.Join(", ", fields.Select(o => $"conf.{o.name}"))})",
                resultName = GetUniqueName("result", paramNames),
                langName = GetUniqueName("lang", paramNames),
            });
        }

        return result;
    }

    private static string GetUniqueName(string name, List<string> takenNames)
    {
        while (takenNames.Contains(name)) name = "_" + name;
        return name;
    }

    /// <summary>
    /// 多个Excel中允许存在同名Sheet（数据会合并导出），但它们生成的代码必须一致
    /// </summary>
    private static bool CheckSameNameSheets(List<string> files, string platform)
    {
        //entityName(含variant) => (xls, sheetName, signature)
        var dict = new Dictionary<string, (string xls, string sheetName, List<string> signature)>();
        var result = true;

        foreach (var xls in files)
        {
            ExporterUtils.ReadExcel(xls, excelReader =>
            {
                foreach (var sheet in excelReader.Workbook.Worksheets)
                {
                    var name = sheet.Name;
                    if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;
                    if (string.IsNullOrEmpty(ExporterUtils.GetVariantMainName(name))) continue;

                    var rowColumn = ExporterUtils.GetRowColumn(sheet);
                    var signature = new List<string>();
                    if (BuildFields(xls, sheet, rowColumn.x, platform, null, signature) == null)
                    {
                        result = false;
                        continue;
                    }

                    var key = ExporterUtils.EntityNameFromTable(sheet, true);
                    if (!dict.TryGetValue(key, out var first))
                    {
                        dict[key] = (xls, name, signature);
                        continue;
                    }

                    var diff = DiffSignature(first.signature, signature);
                    if (diff == null) continue;

                    ExporterUtils.Error($"同名Sheet的表头不一致（平台：{platform}）：" +
                                        $"\n\t前者：{first.xls} => {first.sheetName}" +
                                        $"\n\t后者：{xls} => {name}{diff}");
                    result = false;
                }
            });
        }

        return result;
    }

    /// <summary>
    /// 只比较字段集合，不要求列顺序一致（数据按字段名反序列化，代码按第一个文件的顺序生成）
    /// </summary>
    private static string? DiffSignature(List<string> a, List<string> b)
    {
        var onlyA = a.Except(b).ToList();
        var onlyB = b.Except(a).ToList();
        if (onlyA.Count == 0 && onlyB.Count == 0) return null;

        var sb = new StringBuilder();
        if (onlyA.Count > 0) sb.Append($"\n\t仅前者存在：{string.Join(", ", onlyA)}");
        if (onlyB.Count > 0) sb.Append($"\n\t仅后者存在：{string.Join(", ", onlyB)}");
        return sb.ToString();
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
        _typeResolvers.AddRange(TypeResolverHelper.CreateAll());
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