namespace Tracery.Tokens {

    internal class ActionToken : GrammarToken {
        public string Key;

        protected override string innerText {
            get {
                var text = Resolved ?? Raw;
                return text.Substring(1, text.Length - 2);
            }
        }

        public ActionToken(Unparser grammar, int start, GrammarToken parent) : base(grammar, start, parent) {
            Type = TagType.Action;
        }

        protected override void AddCharInner(char ch) {
            switch (ch) {
                case ':':
                    // Occurrances of ':' within actions separate key from value(s).
                    AddIndexOfInterest();
                    break;
                case ',':
                    if (IndecesOfInterest == null) {
                        // If we haven't hit a ':' yet, then commas mean nothing.
                        break;
                    }
                    AddIndexOfInterest();
                    break;
                default:
                    break;
            }
            Raw += ch;
        }

        public override bool Resolve() {
            if (!base.Resolve()) {
                return false;
            }
            Key = GetSubstringOfInterest(0);
            if (Key == Resolved) {
                // No separating ":", just resolving contents itself.
                Key = null;
            } else {
                // Remove opening '[' from key.
                Key = Key.Substring(1);
                var numOptions = IndecesOfInterest == null ? 0 : IndecesOfInterest.Count;
                var options = new string[numOptions];
                for (var i = 1; i <= numOptions; i++) {
                    options[i - 1] = GetSubstringOfInterest(i);
                }
                // Remove closing ']' from last option.
                var last = options[options.Length - 1];
                last = last.Substring(0, last.Length - 1);
                options[options.Length - 1] = last;
                // If action is "POP", pop the selected key instead of saving it.
                if (options.Length == 1 && options[0] == "POP") {
                    Grammar.PopAction(Key);
                    Key = "";
                } else {
                    Grammar.PushAction(Key, options);
                }
            }

            Resolved = "";
            return true;
        }

        public override void PopRule() {
            Grammar.PopAction(Key);
            if (Children != null) {
                Children.ForEach(child => child.PopRule());
            }
        }
    }

}