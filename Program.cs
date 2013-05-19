using System;

namespace Compiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = ParserWrapper.Parse(@"E:\Presentations\2013-05 - Compiler\notsosimple.vsl", Console.Out);
            Print(parser.RootNode, 0);
            Console.WriteLine("Done");
            Console.ReadKey();
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
