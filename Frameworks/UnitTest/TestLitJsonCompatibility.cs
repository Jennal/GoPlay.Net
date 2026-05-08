using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GoPlay.Core.Encodes;
using NUnit.Framework;

namespace UnitTest
{
    public class TestLitJsonCompatibility
    {
        [Test]
        public void JsonEncoderUsesLocalLitJsonAtRuntime()
        {
            var source = new LitJsonPayload
            {
                Value = "LitJson compatibility",
                Count = 5
            };

            var bytes = JsonEncoder.Instance.Encode(source);
            var decoded = JsonEncoder.Instance.Decode<LitJsonPayload>(bytes);

            Assert.AreEqual(source.Value, decoded.Value);
            Assert.AreEqual(source.Count, decoded.Count);
            Assert.Greater(bytes.Length, 0);

            var size = JsonEncoder.Instance.GetEncodedSize(source);
            Assert.AreEqual(bytes.Length, size);

            var dest = new byte[size];
            var written = JsonEncoder.Instance.EncodeTo(source, dest);

            Assert.AreEqual(size, written);
            CollectionAssert.AreEqual(bytes, dest);

            var litJsonAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "LitJson");

            Assert.NotNull(litJsonAssembly);
        }

        public class LitJsonPayload
        {
            public string Value { get; set; } = string.Empty;
            public int Count { get; set; }
        }

#if NET7_0
        [Test]
        public void CoreBuildsForNetStandard21WithLocalLitJson()
        {
            var repoRoot = GetRepoRoot();
            var coreProject = Path.Combine(repoRoot, "Frameworks", "Core", "Core.csproj");
            var outputDir = Path.Combine(repoRoot, "Frameworks", "Core", "bin", "Debug", "netstandard2.1");

            var result = RunDotNetBuild(coreProject, "netstandard2.1");

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.True(File.Exists(Path.Combine(outputDir, "GoPlay.Core.dll")), result.Output);
            Assert.True(File.Exists(Path.Combine(outputDir, "LitJson.dll")), result.Output);
        }

        private static string GetRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var coreProject = Path.Combine(directory.FullName, "Frameworks", "Core", "Core.csproj");
                if (File.Exists(coreProject))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            Assert.Fail("Could not locate repository root from test output directory.");
            return string.Empty;
        }

        private static (int ExitCode, string Output) RunDotNetBuild(string projectPath, string targetFramework)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add(targetFramework);
            startInfo.ArgumentList.Add("--nologo");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Assert.Fail("Failed to start dotnet build process.");
                return (-1, string.Empty);
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(60000))
            {
                process.Kill(entireProcessTree: true);
                Assert.Fail("dotnet build timed out.");
            }

            return (process.ExitCode, output + error);
        }
#endif
    }
}
