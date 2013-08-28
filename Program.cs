// This compiler is strongly inspired by the excellent exercises created by Jan Christian Meyer
// for Anne C. Elster's course TDT4205 at the Norwegian University of Science and Technology.

using System;
using System.IO;

namespace Compiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string sourceFilePath = (args.Length >= 1 ? args[0] : @"E:\Presentations\2013-05 - Compiler\primes.vsl");
            string outputFilePath = (args.Length >= 2 ? args[1] : @"E:\Presentations\2013-05 - Compiler\output.il");
            var parser = ParserWrapper.Parse(sourceFilePath, Console.Out);
            var symbolTable = new SymbolTable();
            symbolTable.FindSymbols(parser.RootNode);
            Print(parser.RootNode, 0);
            using (var file = File.OpenWrite(outputFilePath))
            using (var writer = new StreamWriter(file))
            {
                var generator = new CilCodeGenerator(writer);
                generator.Generate(parser.RootNode);
            }
            Console.WriteLine("Code generation complete");
        }

        private static void Print(ISyntaxNode node, int level)
        {
            Console.Write(new string(' ', 2 * level));
            Console.WriteLine(node);
            foreach (var child in node.GetChildren())
            {
                Print(child, level + 1);
            }
        }
    }
}
