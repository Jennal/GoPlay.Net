using GoPlay.Common.Data;

namespace GoPlay.Generators.Config
{
    public static class ExporterConsts
    {
        public const string exportPrefix = "#";
        public static readonly string[] exportEnumPrefix = new string[]{"&", "%"};

        public static string splitOuter => RunArgs.Config.ArraySplitOuter;
        public static string splitInner => RunArgs.Config.ArraySplitter;
        
        public const string exportVariantSplit = "@";
        public const string defaultVariant = "zh_cn";

        public static readonly string cacheFile = ".exportCache";

        public static readonly string csFolder = "Generated";
        public static readonly string enumFolder = "Generated/Enum";
        public static readonly string mgrFile = "Generated/Manager/ConfigData.cs";

        public static readonly string dataFolder = "Res/Config";
        
        public const string confClassSuffix = "Conf";

        public static readonly string[] extensionPattern =
        {
            ".xlsx",
            ".xlsm"
        };

        public static readonly string[] ignorePattern =
        {
            "~$",
        };

        public const int LINE_TABLE_DESC = 1;
        public const int LINE_TABLE_PLATFORM = 2;
        public const int LINE_FIELD_DESC = 3;
        public const int LINE_FIELD_NAME = 4;
        public const int LINE_FIELD_TYPE = 5;
        public const int LINE_START = 6;
    }
}