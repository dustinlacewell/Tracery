using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;


namespace Tracery {

    public class Grammar {

        private readonly RuleSet RuleSet;

        public Grammar(RuleSet ruleset) {
            RuleSet = ruleset;
        }

        public bool HasImports => RuleSet.ContainsKey("@import");
        public List<string> Imports => HasImports ? RuleSet["@import"] : new List<string>();

        public static Grammar FromFile(string filename) {
            var source = File.ReadAllText(filename);
            var grammar = FromJson(source);
            grammar.ResolveImports(filename);
            return grammar;
        }

        public static Grammar FromJson(string json) {
            var ruleSet = JsonConvert.DeserializeObject<RuleSet>(json);
            var grammar = new Grammar(ruleSet);
            return grammar;
        }

        public bool HasRule(string rule) {
            return RuleSet.ContainsKey(rule);
        }

        public List<string> GetRule(string rule) {
            return HasRule(rule) ? RuleSet[rule] : new List<string>();
        }

        private void ResolveImports(string filename = null) {
            if (!HasImports) {
                return;
            }
            
            Console.WriteLine($"Initial filename: {filename}");

            var path = Path.GetDirectoryName(filename) ?? Directory.GetCurrentDirectory();
            
            Console.WriteLine($"Import path: {path}");

            Imports.Select(import => Path.Combine(path, import))
                   .Select(FromFile)
                   .Select(g => g.RuleSet)
                   .SelectMany(rs => rs.ToList())
                   .ToList()
                   .ForEach(r => RuleSet[r.Key] = r.Value);

            RuleSet.Remove("@import");
        }

    }

}