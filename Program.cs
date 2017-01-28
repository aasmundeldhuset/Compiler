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
            string targetLanguage = args[0];
            string sourceFilePath = args[1];
            string outputFilePath = args[2];
            var parser = ParserWrapper.Parse(sourceFilePath, Console.Out);
            var symbolTable = new SymbolTable();
            symbolTable.FindSymbols(parser.RootNode);
            Print(parser.RootNode, 0);
            File.Delete(outputFilePath);
            using (var file = File.OpenWrite(outputFilePath))
            using (var writer = new StreamWriter(file))
            {
                ICodeGenerator generator;
                if (targetLanguage == "cil")
                {
                    generator = new CilCodeGenerator(writer);
                }
                else if (targetLanguage == "jasmin")
                {
                    generator = new JasminCodeGenerator(writer);
                }
                else if (targetLanguage == "javascript")
                {
                    generator = new JavaScriptCodeGenerator(writer);
                }
                else if (targetLanguage == "x86")
                {
                    generator = new X86CodeGenerator(writer);
                }
                else
                {
                    Console.WriteLine("Unsupported target language: '{0}' (must be 'cil', 'jasmin', 'javascript', or 'x86')", targetLanguage);
                    return;
                }
                generator.Generate(parser.RootNode);
            }
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
