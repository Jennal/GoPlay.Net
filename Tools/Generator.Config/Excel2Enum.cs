using System.Globalization;
using System.Text;
using DotLiquid;
using Generator.Core;
using OfficeOpenXml;

namespace GoPlay.Generators.Config;

public class Excel2Enum
{
    public static Dictionary<string, List<(string, int)>> Enums = new Dictionary<string, List<(string, int)>>();
    
    private static string CLASS_TEMPLETE = GeneratorUtils.GetTpl("tpl_class_enum");
    
    public static void Generate(string xlsFolder, string csFolder, bool isDryRun, string tplPath="")
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        if (!Directory.Exists(xlsFolder))
        {
            ExporterUtils.Error("Excel目录不存在：" + xlsFolder + "\n请设置后再试...");
            return;
        }

        if (!CheckConflictNames(xlsFolder)) return;

        var tpl = string.IsNullOrEmpty(tplPath) ? CLASS_TEMPLETE : File.ReadAllText(tplPath);
        var files = Directory.EnumerateFiles(xlsFolder, "*.*")
            .Where(p => ExporterConsts.extensionPattern.Any(p.EndsWith))
            .Where(xls => !xls.EndsWith(".converting") &&
                          !ExporterConsts.ignorePattern.Any(o => Path.GetFileName(xls).StartsWith(o)))
            .ToList();
        for (var i = 0; i < files.Count; i++)
        {
            var xls = files[i];
            var tmpFileName = xls + ".converting";
            if (File.Exists(tmpFileName)) File.Delete(tmpFileName);
            File.Copy(xls, tmpFileName);

            try
            {
                using (var stream = File.Open(tmpFileName, FileMode.Open, FileAccess.Read))
                {
                    var excelReader = new ExcelPackage(stream);
                    foreach (var sheet in excelReader.Workbook.Worksheets)
                    {
                        var name = sheet.Name;
                        if (name == null || !ExporterConsts.exportEnumPrefix.Any(o => name.StartsWith(o))) continue;

//                            Debug.Log($"{xls} => {name}");   
                        ExporterUtils.Info(
                            $"正在导出 {Path.GetFileNameWithoutExtension(xls)} => {name.Substring(ExporterConsts.exportPrefix.Length)} ...");
                        ConvertToEnum(csFolder, xls, sheet, isDryRun, tpl);
                    }
                }
            }
            finally
            {
                File.Delete(tmpFileName);
            }
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
                        if (name == null || !ExporterConsts.exportEnumPrefix.Any(o => name.StartsWith(o)) ) continue;

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

    static void ConvertToEnum(string enumFolder, string xls, ExcelWorksheet table, bool isDryRun, string tpl)
    {
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        if (string.IsNullOrEmpty(tableName))
        {
            ExporterUtils.Error($"[错误]表名不存在：{xls} => {table.Name}");
            return;
        }

        var rowColumns = ExporterUtils.GetRowColumn(table);
        ConvertTable(enumFolder, xls, table, rowColumns, isDryRun, tpl);
    }

    static void ConvertTable(string enumFolder, string xls, ExcelWorksheet table, Vector2Int rowColumn, bool isDryRun, string tpl)
    {
        var tableName = table.Name.Substring(ExporterConsts.exportPrefix.Length);
        if (rowColumn.x <= 0 || rowColumn.y <= 0) return;

        var entityName = table.Name.Substring(1);
        var isFlag = table.Name.StartsWith("&");

        var tplData = new TemplateEnumData
        {
            excelFile = xls,
            tableName = tableName,
            desc = table.Cells[1, 1].GetValue<string>().Split("\n"),
            isFlags = isFlag,
            name = entityName,
            fields = new List<TemplateEnumField>(),
        };

        for (var i = 3; i <= rowColumn.y; i++)
        {
            var val = table.Cells[i, 1].GetValue<string>();
            var name = table.Cells[i, 2].GetValue<string>();
            var comment = table.Cells[i, 3].GetValue<string>();

            tplData.fields.Add(new TemplateEnumField
            {
                name = name,
                value = val,
                desc = comment?.Split("\n"),
            });
        }

        Enums[entityName] = tplData.fields.Select(o =>
        {
            var name = o.name;
            var val = 0;

            if (o.value.StartsWith("0x"))
            {
                val = Convert.ToInt32(o.value, 16);
            }
            else
            {
                val = int.Parse(o.value);
            }

            return (name, val);
        }).ToList();

        if (!isDryRun)
        {
            var content = GeneratorUtils.RenderTpl(tpl, new { data = tplData });
            WriteEntityFile(enumFolder, entityName, content);
        }
    }

    static string WriteEntityFile(string enumFolder, string entityName, string content)
    {
        ExporterUtils.Log("写入文件：" + entityName);
        var path = Path.Combine(enumFolder, ExporterConsts.enumFolder, entityName + ".cs");
        ExporterUtils.CreateFileFolderIfNotExists(path);
        File.Delete(path);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    public static void Clear(string csFolder)
    {
        var folderPath = Path.Combine(csFolder, ExporterConsts.csFolder);
        if (!Directory.Exists(folderPath)) return;

        Directory.Delete(folderPath, true);
    }
}