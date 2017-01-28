using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Compiler
{
    public class X86CodeGenerator : ICodeGenerator, ISyntaxNodeVisitor
    {
        private readonly TextWriter _output;
        private int _labelCount;
        private int? _innermostWhileLabelIndex;
        private readonly Stack<int> _parameterCounts = new Stack<int>();
        private bool _currentFunctionIsMain;

        private const string TEXT_HEAD = @"
.text
.globl main
main:
    pushl %ebp           /* Store the base of the caller (shell) stack frame */
    movl  %esp, %ebp     /* Make my own stack frame */
    movl  8(%esp), %esi  /* Store the first parameter (argc) in ESI */
    decl  %esi           /* argc--; argv[0] is not interesting to us */
    jz    noargs         /* Skip argument setup if there are none */

    movl  12(%ebp), %ebx /* Store the base addr. of argv in EBX */
pusharg:                 /* Loop over the arguments */
    addl  $4, %ebx       /* Look at the next argument (disregarding argv[0]) */
    pushl $10            /* strtol arg 3: our number base is 10 */
    pushl $0             /* strtol arg 2: there is no error pointer */
    pushl (%ebx)         /* strtol arg 1: Addr. of string containing integer */
    call  strtol         /* Call strtol, to convert the string to a 32-bit int */
    addl  $12, %esp      /* Restore the stack pointer to before strtol-params */
    pushl %eax           /* Push return value from strtol (our new argument) */
    decl  %esi           /* Decrement the number of arguments to go */
    jnz   pusharg        /* Loop if there are any arguments left */
noargs:
    call fun_{0}         /* Args now on stack, call entry */
    leave                /* Give the stack frame back to the calling shell */
    ret                  /* Return value from vsl program is still in EAX */
";

        public X86CodeGenerator(TextWriter output)
        {
            _output = output;
        }

        public void Generate(ProgramNode program)
        {
            throw new NotImplementedException();
        }

        public void Visit(BlockStatementNode block)
        {
            throw new NotImplementedException();
        }

        public void Visit(ReturnStatementNode statement)
        {
            throw new NotImplementedException();
        }

        public void Visit(IfStatementNode statement)
        {
            throw new NotImplementedException();
        }

        public void Visit(PrintStatementNode print)
        {
            throw new NotImplementedException();
        }

        public void Visit(BinaryExpressionNode expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(FunctionCallNode call)
        {
            throw new NotImplementedException();
        }

        public void Visit(VariableReferenceNode reference)
        {
            throw new NotImplementedException();
        }

        public void Visit(IdentifierNode identifier)
        {
            throw new NotImplementedException();
        }

        public void Visit(StringNode str)
        {
            throw new NotImplementedException();
        }

        public void Visit(IntegerNode integer)
        {
            throw new NotImplementedException();
        }

        public void Visit(DeclarationNode declaration)
        {
            throw new NotImplementedException();
        }

        public void Visit(ConstantExpressionNode constant)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryExpressionNode expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(InputStatementNode input)
        {
            throw new NotImplementedException();
        }

        public void Visit(WhileStatementNode statement)
        {
            throw new NotImplementedException();
        }

        public void Visit(NullStatementNode statement)
        {
            throw new NotImplementedException();
        }

        public void Visit(AssignmentStatementNode assignment)
        {
            throw new NotImplementedException();
        }

        public void Visit(FunctionNode function)
        {
            throw new NotImplementedException();
        }

        public void Visit(ProgramNode program)
        {
            throw new NotImplementedException();
        }
    }
}
