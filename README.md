# Tracery for .net

This is an implementation of [Kate Compton](https://twitter.com/galaxykate)'s [Tracery](http://tracery.io/) for .net

It as a fork of a C# implementation [for Unity](https://assetstore.unity.com/packages/tools/input-management/tracery-100911) by [5ive Bullet Games](https://twitter.com/5ivebullets).

Tracery utilizes a simple(ish) grammar anyone can write:

    {
        "origin": ["Once upon a time, #who# #did# #what#. The end."],
        "who": ["I", "you", "they"],
        "did": ["ate", "cuddled", "gift-wrapped"],
        "what": ["a cat", "the moon", "the president"]
    }

Which can then be used to generate text:

    using System;
    using System.IO;

    using Tracery;

    namespace Tracery.CLI {

        class Program {

            private static void Main(string[] args) {
                var grammar = File.ReadAllText("grammar.json");
                var parser = new Parser(grammar);
                var output = parser.Generate();
                Console.WriteLine(output);
            }
        }
    }

Here's a few examples:

    Once upon a time, they gift-wrapped the moon. The end.

    Once upon a time, I cuddled the president. The end.

    Once upon a time, you ate the moon. The end.

    Once upon a time, they gift-wrapped a cat. The end.

Try it yourself by running the following command from the root of the repository:

    dotnet run --project Tracery.CLI