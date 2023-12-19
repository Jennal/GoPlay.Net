using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using GoPlay.Generators.Config;
using GoPlay.Generators.Extension;

namespace GoPlay;

class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();
        {
            rootCommand.AddCommand(CreateInfoCommand());
            rootCommand.AddCommand(CreateConfigCommand());
            rootCommand.AddCommand(CreateGenExtension());
        }
        await rootCommand.InvokeAsync(args);
    }

    private static Command CreateInfoCommand()
    {
        var cmd = new Command("info", "信息显示");
        {
            cmd.Handler = CommandHandler.Create(
                () =>
                {
                    Console.WriteLine($"CommandLine: {Environment.CommandLine}");
                    Console.WriteLine($"ProcessPath: {Environment.ProcessPath}");
                    Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");
                    Console.WriteLine($"SystemDirectory: {Environment.SystemDirectory}");
                });
        }
        return cmd;
    }

    #region Gen

    private static Command CreateGenExtension()
    {
        var cmd = new Command("extension", "extension扩展代码生成");
        {
            cmd.AddOption(new Option<string>(new []{"-i", "--input-sln-folder"}, "输入Solution目录"));
            cmd.AddOption(new Option<string>(new []{"-of", "--output-frontend-file"}, "输出前端扩展cs文件路径"));
            cmd.AddOption(new Option<string>(new []{"-ob", "--output-backend-file"}, "输出后端扩展cs文件路径"));
            cmd.AddOption(new Option<string>(new []{"-b", "--base-classes"}, () => "ProcessorBase", "输入父类用于过滤：ProcessorBase,MyProcessor"));
            cmd.AddOption(new Option<string>(new []{"-tf", "--template-frontend-file"}, () => "", "输出前端扩展的liquid模板文件路径"));
            cmd.AddOption(new Option<string>(new []{"-tb", "--template-backend-file"}, () => "", "输出后端扩展的liquid模板文件路径"));
            cmd.AddOption(new Option<string>(new []{"-nf", "--namespace-frontend"}, () => "", "输出前端，增加的using namespace，多个namespace用逗号隔开，例如：System,MyNamespace"));
            cmd.AddOption(new Option<string>(new []{"-nb", "--namespace-backend"}, () => "", "输出前端，增加的using namespace，多个namespace用逗号隔开，例如：System,MyNamespace"));
            cmd.AddOption(new Option<string>(new []{"-igt", "--ignore-types"}, () => "", "类名忽略列表，多个用逗号隔开，例如：EchoProcessor,TestProcessor"));
            cmd.AddOption(new Option<string>(new []{"-igm", "--ignore-methods"}, () => "", "函数名忽略列表，多个用逗号隔开，例如：EchoProcessor,TestProcessor"));

            //参数名要和Option名字对应，例如：inFolder 对应 --in-folder
            cmd.Handler = CommandHandler.Create(
                (string inputSlnFolder, string outputFrontendFile, string outputBackendFile, string baseClasses, string templateFrontendFile, string templateBackendFile, string namespaceFrontend, string namespaceBackend, string ignoreTypes, string ignoreMethods) =>
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    Processors2Extension.Generate(inputSlnFolder, outputFrontendFile, outputBackendFile, baseClasses, templateFrontendFile, templateBackendFile, namespaceFrontend, namespaceBackend, ignoreTypes, ignoreMethods).Wait();
                    watch.Stop();
                    Console.WriteLine($"Time Elapsed: {watch.Elapsed}");
                    Console.WriteLine("========== Generate Extension Finished ==========");
                });
        }
        return cmd;
    }

    #endregion
    
    #region Config

    private static Command CreateConfigCommand()
    {
        var cmd = new Command("config", "将Excel导出成代码和YAML数据");
        {
            cmd.AddOption(new Option<string>(new[] {"-i", "--in-folder"}, () => "../../Excels", "Excel目录"));
            cmd.AddOption(new Option<string>(new[] {"-oc", "--out-code-folder"}, () => "", "代码保存项目目录"));
            cmd.AddOption(new Option<string>(new[] {"-od", "--out-data-folder"}, () => "", "数据保存项目目录"));
            cmd.AddOption(new Option<string>(new[] {"-p", "--platform"}, () => "s", "平台: s | c"));
            cmd.AddOption(new Option<bool>(new[] {"-f", "--force"}, () => false, "强制重新导出"));
            cmd.AddOption(new Option<bool>(new[] {"-c", "--clear-old"}, () => false, "删除旧脚本和数据"));
            cmd.AddOption(new Option<string>(new[] {"-tc", "--template-conf"}, () => "", "Conf代码模板的liquid文件路径"));
            cmd.AddOption(new Option<string>(new[] {"-tm", "--template-manager"}, () => "", "Manager代码模板的liquid文件路径"));
            cmd.AddOption(new Option<string>(new[] {"-te", "--template-enum"}, () => "", "Enum代码模板的liquid文件路径"));

            //参数名要和Option名字对应，例如：inFolder 对应 --in-folder
            cmd.Handler = CommandHandler.Create(
                (string inFolder, string outCodeFolder, string outDataFolder, string platform, bool force, bool clearOld, string templateConf, string templateManager, string templateEnum) =>
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    {
                        if (force) ExportCache.Remove(inFolder, platform);
                        if (clearOld)
                        {
                            if (!string.IsNullOrEmpty(outCodeFolder))
                            {
                                Excel2Enum.Clear(outCodeFolder);
                                Excel2Script.Clear(outCodeFolder);
                            }

                            Excel2Yaml.Clear(outDataFolder);
                        }

                        if (!string.IsNullOrEmpty(outCodeFolder))
                        {
                            Excel2Enum.Generate(inFolder, outCodeFolder, false, templateEnum);
                            Excel2Script.Generate(inFolder, outCodeFolder, platform, templateConf, templateManager);
                        }
                        else
                        {
                            Excel2Enum.Generate(inFolder, outCodeFolder, true, templateEnum);
                        }

                        Excel2Yaml.Generate(inFolder, outDataFolder, platform);
                    }
                    watch.Stop();
                    Console.WriteLine($"Time Elapsed: {watch.Elapsed}");
                    Console.WriteLine("========== Generate Config Finished ==========");
                });
        }
        return cmd;
    }
    
    #endregion
}