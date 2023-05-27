using OfficeOpenXml;

namespace GoPlay.Generators.Config
{
    public abstract class ScriptGenHookBase
    {
        public abstract bool Recognize(string xls, ExcelWorksheet table, Vector2Int rowColumn);
        public abstract void OnExportBegin(string xls, ExcelWorksheet table, Vector2Int rowColumn);
        public abstract void OnExportFinish(string xls, ExcelWorksheet table, Vector2Int rowColumn, string codePath);
        public abstract void OnExportAllFinished();
    }
}