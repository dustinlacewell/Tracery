using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Tracery.Tokens;

using foo = System.Nullable<int>;


namespace Tracery {

    /// <summary>
    /// A Grammar object as described in @GalaxyKate's tracery.io.
    /// </summary>
    public class Unparser {

        /// <summary>
        /// Finds escape characters that are not preceeded by an odd number of escape characters.
        /// </summary>
        private static readonly Regex non_escaped_backslash_regex =
            new Regex(@"(?<=(?:\\(?(esc)(?<-esc>)|(?<esc>)))*)(?(esc)(?!))\\(?!\\)");

        /// <summary>
        /// Key/value store for grammar rules.
        /// </summary>
        private Grammar _grammar;

        /// <summary>
        /// Key/value store for savable data.
        /// </summary>
        private ActionCache _saveData;

        /// <summary>
        /// Modifier function table.
        /// </summary>
        public readonly ModifierTable Modifiers = new ModifierTable {
            {"a", Tracery.Modifiers.Article},
            {"beeSpeak", Tracery.Modifiers.BeeSpeak},
            {"capitalize", Tracery.Modifiers.Capitalize},
            {"capitalizeAll", Tracery.Modifiers.CapitalizeAll},
            {"comma", Tracery.Modifiers.Comma},
            {"ed", Tracery.Modifiers.PastTense},
            {"inQuotes", Tracery.Modifiers.InQuotes},
            {"s", Tracery.Modifiers.Pluralize},
            {"titleCase", Tracery.Modifiers.TitleCase}
        };

        public Unparser(Grammar grammar, int? seed = null, ActionCache cache = null) {
            _grammar = grammar;
            _saveData = cache ?? new ActionCache();
            Seed(seed);
        }

        public void Seed(int? seed) {
            if (seed.HasValue) {
                ListExtensions.Seed((int) seed);
            }
        }

        /// <summary>
        /// Resolves the default starting symbol "#origin#".
        /// </summary>
        /// <param name="randomSeed">Reliably seeds "random" selection to provide consistent results.</param>
        /// <returns>A result interpreted from the symbol "#origin#".</returns>
        public string Generate(int? seed = null) {
            return Generate("#origin#", seed);
        }

        public string Generate(string input, int? seed = null) {
            Seed(seed);

            var output = ParseInner(input);
            // Since we kept escape characters in the string, replace single escape characters
            output = non_escaped_backslash_regex.Replace(output, "");
            // Now replace all double-escapes with single ones.
            output = output.Replace(@"\\", @"\");
            // Clear any top-level saved actions.
            _saveData.Clear();
            return output;
        }

        internal string ParseInner(string input) {
            if (string.IsNullOrEmpty(input)) {
                return input;
            }

            var output = "";
            var escaped = false;

            GrammarToken token = null;

            for (var i = 0; i < input.Length; i++) {
                var ch = input[i];

                if (escaped) {
                    AddCharToToken(ch, ref output, token, true);
                    escaped = false;
                    continue;
                }

                if (ch == '\\') {
                    AddCharToToken(ch, ref output, token, true);
                    escaped = true;
                    continue;
                }

                switch (ch) {
                    case '[':
                        // Start new action token.
                        var newToken = new ActionToken(this, i + 1, token);

                        if (token == null) {
                            token = newToken;
                        } else {
                            token.AddChild(newToken);
                        }

                        newToken.AddChar(ch);
                        break;

                    case ']':
                        // Close highest action token. If any inner tokens are unfinished, they resolve to their raw text.
                        var action = token?.FindLowestOpenOfType(TagType.Action);

                        if (action == null) {
                            // No open action. Add ] to text as normal.
                            AddCharToToken(ch, ref output, token);
                            break;
                        } else {
                            action.AddChar(ch);
                        }

                        action.Resolve();
                        break;

                    case '#':
                        // If lowest open node is a tag, close it. Otherwise, open a new tag.
                        if (token == null) {
                            token = new TagToken(this, i + 1, null);
                            token.AddChar(ch);
                            break;
                        }

                        var lowest = token.FindLowestOpenToken();

                        if (lowest.Type == TagType.Tag) {
                            lowest.AddChar(ch);
                            lowest.Resolve();
                            break;
                        }

                        var newTag = new TagToken(this, i + 1, lowest);
                        lowest.AddChild(newTag);
                        newTag.AddChar(ch);
                        break;

                    default:
                        AddCharToToken(ch, ref output, token);
                        break;
                }

                if (token != null && token.IsResolved) {
                    output += token.Resolved;
                    token = null;
                }
            }

            return output;
        }

        /// <summary>
        /// Add a modifier to the modifier lookup.
        /// </summary>
        /// <param name="name">The name to identify the modifier with.</param>
        /// <param name="func">A method that returns a string and takes a string as a param.</param>
        public void AddModifier(string name, Func<string, string> func) {
            Modifiers[name] = func;
        }

        private static void AddCharToToken(char ch, ref string output, GrammarToken token, bool escaped = false) {
            if (token == null) {
                output += ch;
                return;
            }

            token.FindLowestOpenToken().AddChar(ch, escaped);
        }

        /// <summary>
        /// Applies found modifier functions to a symbol.
        /// </summary>
        /// <param name="baseText">Text to which to apply modifiers.</param>
        /// <param name="modifiers">Names of modifiers to apply.</param>
        /// <return>The first entry modified by modifiers found.</return>
        internal string ApplyModifiers(string baseText, IEnumerable<string> modifiers) {
            return modifiers.Where(modName => Modifiers.ContainsKey(modName))
                            .Select(modName => Modifiers[modName]).Aggregate(baseText, (cume, mod) => mod(cume));
        }


        /// <summary>
        /// Resolve the #symbol# from save data first, then a grammar rule if nothing saved,
        /// or finally just return the string itself if no saved or grammar substitution.
        /// </summary>
        /// <param name="symbol">The string to match against.</param>
        /// <returns>The interpreted string from save data or grammar, if any.</returns>
        internal string ResolveSymbol(string symbol) {
            if (string.IsNullOrEmpty(symbol)) {
                return "";
            }

            if (symbol[0] == '#') {
                symbol = symbol.Substring(1);
            }

            if (string.IsNullOrEmpty(symbol)) {
                return "";
            }

            if (symbol[symbol.Length - 1] == '#') {
                symbol = symbol.Substring(0, symbol.Length - 1);
            }

            if (string.IsNullOrEmpty(symbol)) {
                return "";
            }

            if (_saveData.ContainsKey(symbol)) {
                return _saveData[symbol][_saveData[symbol].Count - 1].PickRandom();
            }

            if (_grammar.HasRule(symbol)) {
                return _grammar.GetRule(symbol).PickRandom();
            }

            return symbol;
        }

        /// <summary>
        /// Resolves and saves the content inside [action] brackets.
        /// </summary>
        /// <param name="key">Key under which to save action options.</param>
        /// <param name="options">Items to save as options for the given key.</param>
        internal void PushAction(string key, params string[] options) {
            var values = options.ToList();

            if (!_saveData.ContainsKey(key)) {
                _saveData[key] = new List<List<string>>();
            }

            _saveData[key].Add(values);
        }

        /// <summary>
        /// Pops the most recent entry for the save data key.
        /// </summary>
        /// <param name="key">Names of action to pop.</param>
        internal void PopAction(string key) {
            if (key == null) {
                return;
            }

            if (!_saveData.ContainsKey(key)) {
                return;
            }

            _saveData[key].RemoveAt(_saveData[key].Count - 1);

            if (_saveData[key].Count == 0) {
                _saveData.Remove(key);
            }
        }

    }

}