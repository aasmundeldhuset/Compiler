using System.IO;

namespace Compiler
{
    public static class ParserWrapper
    {
        public static Parser Parse(string filePath, TextWriter errorStream)
        {
            using (var file = File.OpenRead(filePath))
            {
                var scanner = new Scanner(file);
                var parser = new Parser(scanner);
                if (errorStream != null) parser.errors.errorStream = errorStream;
                parser.Parse();
                return parser;
            }
        }
    }
}
