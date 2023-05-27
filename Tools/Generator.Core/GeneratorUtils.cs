using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;

namespace Generator.Core
{
    public static class GeneratorUtils
    {
        private static Dictionary<string, object> _tpl_cache = new Dictionary<string, object>();
        
        public static string[] GetBasicConf(string key)
        {
            if (!_tpl_cache.ContainsKey(key))
            {
                var cmd = Environment.CommandLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var dir = Path.GetDirectoryName(cmd[0]);
                var path = Path.Combine(dir, "Res", key + ".txt");
                var lines = File.ReadAllLines(path);
                _tpl_cache[key] = lines.Where(o => !o.Trim().StartsWith("#")).ToArray();
            }

            return (string[]) _tpl_cache[key];
        }

        public static string GetTpl(string key)
        {
            if (!_tpl_cache.ContainsKey(key))
            {
                var cmd = Environment.CommandLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var dir = Path.GetDirectoryName(cmd[0]);
                var path = Path.Combine(dir, "Res", key + ".liquid");
                _tpl_cache[key] = File.ReadAllText(path);
            }

            return (string) _tpl_cache[key];
        }

        public static string RenderTpl(string tpl, object data)
        {
            Template.DefaultSyntaxCompatibilityLevel = SyntaxCompatibility.DotLiquid22;
            var template = Template.Parse(tpl);
            var hash = Hash.FromAnonymousObject(data);
            return template.Render(hash);
        }
    }
}