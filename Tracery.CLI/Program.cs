using System;
using System.IO;


namespace Tracery.CLI {

    class Program {

        private static void Main(string[] args) {
            var grammar = File.ReadAllText("Tracery.Testing/Grammars/readme-grammar.json");
            var unparser = new Unparser(grammar);

            foreach (var i in new[] {0, 1, 2, 3, 4}) {
                var output = unparser.Generate(i);
                Console.WriteLine($"#{i}: {output}\n");
            }
        }

    }

}