using System.IO;

namespace Compiler
{
    public interface ICodeGenerator
    {
        void Generate(ProgramNode program);
    }
}
