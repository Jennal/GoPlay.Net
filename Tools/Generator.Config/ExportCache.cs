using Newtonsoft.Json;
using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public class SheetEntity
    {
        public string Name;
        public string Platform;
    }
    
    public class ExportCacheData
    {
        public DateTime ExportCSharpTime;
        public DateTime ExportDataTime;

        public DateTime ModifiedTime;
        public List<SheetEntity> SheetEntities = new List<SheetEntity>();
    }
    
    public class ExportCache
    {
        public Dictionary<string, ExportCacheData> Dict;
        
        public static ExportCache Load(string xlsFolder, string platform)
        {
            var result = new ExportCache();
            var file = GetPath(xlsFolder, platform);
            if (File.Exists(file))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, ExportCacheData>>(json);
                    result.Dict = data;
                }
                catch
                {
                }
            }

            return result;
        }

        public static void Remove(string xlsFolder, string platform)
        {
            var file = GetPath(xlsFolder, platform);
            if (!File.Exists(file)) return;

            File.Delete(file);
        }
        
        private static string GetPath(string xlsFolder, string platform)
        {
            var file = Path.Combine(xlsFolder, ExporterConsts.cacheFile + $"_{platform}");
            return file;
        }

        public List<SheetEntity> GetSheetEntities(string file)
        {
            if (!Dict.ContainsKey(file)) return null;

            return Dict[file].SheetEntities;
        }
        
        public bool FilterExportCSharp(string outFolder, string file, string platform)
        {
            if (Dict == null) return true;
            if (!Dict.ContainsKey(file)) return true;

            var item = Dict[file];
            if (GetModifyTime(file) >= item.ExportCSharpTime) return true;
            
            foreach (var entity in item.SheetEntities)
            {
                if (!entity.Platform.Contains(platform)) continue;
                
                var mainName = ExporterUtils.GetVariantMainName(entity.Name);
                var filePath = Path.Combine(outFolder, ExporterConsts.csFolder, mainName + "s.cs");
                if (!File.Exists(filePath)) return true;
            }

            return false;
        }
        
        public bool FilterExportScriptableObject(string confFolder, string file, string platform)
        {
            if (Dict == null) return true;
            if (!Dict.ContainsKey(file)) return true;

            var item = Dict[file];
            if (GetModifyTime(file) >= item.ExportDataTime) return true;

            foreach (var entity in item.SheetEntities)
            {
                if (!entity.Platform.Contains(platform)) continue;
                
                var mainName = ExporterUtils.GetVariantMainName(entity.Name);
                var variantName = ExporterUtils.GetVariantName(entity.Name);
                var filePath = Path.Combine(confFolder, ExporterConsts.dataFolder, variantName, mainName + "s.asset");
                if (!File.Exists(filePath)) return true;
            }

            return false;
        }

        public void RefreshExportCSharp(string xlsFolder, string platform, List<string> files)
        {
            RefreshAndSet(xlsFolder, platform, files, item =>
            {
                item.ExportCSharpTime = DateTime.Now;
            });
        }

        public void RefreshExportScriptableObject(string xlsFolder, string platform, List<string> files)
        {
            RefreshAndSet(xlsFolder, platform, files, item =>
            {
                item.ExportDataTime = DateTime.Now;
            });
        }

        private void RefreshAndSet(string xlsFolder, string platform, List<string> files, Action<ExportCacheData> refresh)
        {
            if (Dict == null) Dict = new Dictionary<string, ExportCacheData>();

            foreach (var file in files)
            {
                if (!Dict.ContainsKey(file))
                {
                    Dict[file] = new ExportCacheData();
                }

                refresh(Dict[file]);
            }

            var keys = Dict.Keys.Where(o => !files.Any(f => f == o)).ToList();
            foreach (var key in keys)
            {
                Dict.Remove(key);
            }

            foreach (var item in Dict)
            {
                RefreshEntities(item.Key, item.Value);
            }

            var path = GetPath(xlsFolder, platform);
            var json = JsonConvert.SerializeObject(Dict, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private void RefreshEntities(string file, ExportCacheData data)
        {
            var modifyTime = GetModifyTime(file);
            if (modifyTime <= data.ModifiedTime) return;

            data.ModifiedTime = modifyTime;
            data.SheetEntities.Clear();
            
            var tmpFileName = file + ".converting";
            if (File.Exists(tmpFileName))
            {
                File.Delete(tmpFileName);
            }

            File.Copy(file, tmpFileName);

            try
            {
                using (var stream = File.Open(tmpFileName, FileMode.Open, FileAccess.Read))
                {
                    var excelReader = new ExcelPackage(stream);
                    foreach (var sheet in excelReader.Workbook.Worksheets)
                    {
                        var name = sheet.Name;
                        if (name == null || !name.StartsWith(ExporterConsts.exportPrefix)) continue;

                        var entityName = ExporterUtils.EntityNameFromTable(sheet, true);
                        var platform = ExporterUtils.PlatformsFromTable(sheet);
                        data.SheetEntities.Add(new SheetEntity
                        {
                            Name = entityName,
                            Platform = platform,
                        });
                    }
                }
            }
            finally
            {
                File.Delete(tmpFileName);
            }
        }

        private DateTime GetModifyTime(string file)
        {
            var fileInfo = new FileInfo(file);
            return fileInfo.LastWriteTime;
        }
    }
}