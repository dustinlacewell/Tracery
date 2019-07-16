using System.IO;

using Xunit;
using Xunit.Abstractions;


namespace Tracery.Testing {

    public class GrammarTests {

        public GrammarTests(ITestOutputHelper log) {
            _log = log;
        }

        private readonly ITestOutputHelper _log;

        [Fact]
        public void HeroGrammarWorks() {
            var grammarText = File.ReadAllText("Grammars/hero-grammar.json");
            var grammar = new Unparser(grammarText);
            var output = grammar.Generate(0);
            var test = "Once upon a time, Chiaki the time captain left his home. Chiaki went home.";
            Assert.Equal(test, output);
        }

    }

}