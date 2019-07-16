namespace Tracery.Tokens {

    internal class TagToken : GrammarToken {
        protected override string innerText {
            get {
                var text = Resolved ?? Raw;
                return text.Substring(1, text.Length - 2);
            }
        }

        public TagToken(Unparser grammar, int start, GrammarToken parent) : base(grammar, start, parent) {
            Type = TagType.Tag;
        }

        protected override void AddCharInner(char ch) {
            switch (ch) {
                case '.':
                    AddIndexOfInterest();
                    Raw += ch;
                    break;
                default:
                    Raw += ch;
                    break;
            }
        }

        public override bool Resolve() {
            if (!base.Resolve()) {
                return false;
            }
            // Once text is fully resolved, replace it with the grammar/action rule, if any.
            var finalText = Grammar.ParseInner(Grammar.ResolveSymbol(GetSubstringOfInterest(0)));
            // If there are any modifiers, apply them now.
            if (IndecesOfInterest != null && IndecesOfInterest.Count > 0) {
                var modifiers = new string[IndecesOfInterest.Count];
                for (var i = 1; i <= IndecesOfInterest.Count; i++) {
                    modifiers[i - 1] = GetSubstringOfInterest(i);
                }
                // Remove closing # from last modifier.
                var last = modifiers[modifiers.Length - 1];
                last = last.Substring(0, last.Length - 1);
                modifiers[modifiers.Length - 1] = last;
                finalText = Grammar.ApplyModifiers(finalText, modifiers);
            }
            Resolved = finalText;
            // When a tag is resolved, any of its children that are actions pop their save rule,
            // as those actions are meant to be local to the tag.
            if (Children != null) {
                Children.ForEach(child => child.PopRule());
            }
            return true;
        }
    }

}