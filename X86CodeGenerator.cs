using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compiler
{
    public class X86CodeGenerator : ICodeGenerator, ISyntaxNodeVisitor
    {
        private readonly TextWriter _output;
        private int _labelCount;
        private readonly Stack<int> _whileLabelIndices = new Stack<int>();
        private FunctionNode _currentFunction;
        private static readonly StringNode NEWLINE_NODE = new StringNode("\n");
        private Dictionary<string, int> _strings = new Dictionary<string, int>();
        private static readonly List<Operator> COMPARISON_OPERATORS = new List<Operator> { Operator.Equal, Operator.NotEqual, Operator.LessThan, Operator.LessThanOrEqual, Operator.GreaterThanOrEqual, Operator.GreaterThan };

        public X86CodeGenerator(TextWriter output)
        {
            _output = output;
        }

        public void Generate(ProgramNode program)
        {
            _labelCount = 0;
            _whileLabelIndices.Clear();
            _currentFunction = null;
            _strings.Clear();
            _strings[NEWLINE_NODE.Value] = 0;
            Visit(program);
        }

        public void Visit(ProgramNode program)
        {
            EmitHead();
            foreach (var function in program.Functions)
            {
                function.Accept(this);
            }
            EmitTail();
        }

        public void Visit(FunctionNode function)
        {
            _currentFunction = function;
            EmitBlankLine();
            EmitLabelComment("Function: " + function.Name.Name);
            EmitLabel("fun_" + function.Name.Name);
            //TODO: Replace with enter
            Emit("push", "ebp");
            Emit("mov", "ebp", "esp");
            Emit("sub", "esp", GetStackSpaceForArguments().ToString());

            function.Body.Accept(this);
        }

        public void Visit(BlockStatementNode block)
        {
            EmitComment("Begin block");
            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }
            EmitComment("End block");
        }

        public void Visit(ReturnStatementNode statement)
        {
            statement.Expression.Accept(this);
            EmitComment("Return");
            Emit("pop", "eax");
            Emit("add", "esp", GetStackSpaceForArguments().ToString()); //TODO: Not really necessary
            Emit("leave");
            Emit("ret");
        }

        public void Visit(IfStatementNode statement)
        {
            int labelIndex = _labelCount++;
            string falseLabel = "if_false_" + labelIndex;
            string endLabel = "if_end_" + labelIndex;
            EmitComment("If: condition");
            var condition = RequireComparisonExpression(statement.Condition, "If");
            condition.Accept(this);
            EmitComment("If: jump");
            Emit(GetJumpIfFalseInstruction(condition.Operator), falseLabel);
            EmitComment("If: begin true part");
            statement.ThenBody.Accept(this);
            if (statement.ElseBody != null)
                Emit("jmp", endLabel);
            EmitComment("If: end true part");
            EmitLabel(falseLabel);
            if (statement.ElseBody != null)
            {
                EmitComment("If: begin false part");
                statement.ElseBody.Accept(this);
                EmitComment("If: end false part");
                EmitLabel(endLabel);
            }
        }

        public void Visit(PrintStatementNode print)
        {
            foreach (var item in print.Items)
            {
                if (item is StringNode)
                {
                    PrintString((StringNode)item);
                }
                else
                {
                    PrintInteger(item);
                }
            }
            PrintString(NEWLINE_NODE, "Print final newline");
        }

        public void Visit(UnaryExpressionNode expression)
        {
            // Evaluate child nodes recursively
            expression.Child.Accept(this);

            EmitComment("Unary operator " + expression.Operator);
            if (expression.Operator == Operator.Subtract)
            {
                // Move 0 to eax, subtact TOS from eax, move eax to TOS (can probably be done simpler)
                Emit("mov", "eax", "0");
                Emit("sub", "eax", "[esp]");
                Emit("mov", "[esp]", "eax");
            }
            else
            {
                throw new Exception("Unsupported unary operator: " + expression.Operator);
            }
        }

        public void Visit(BinaryExpressionNode expression)
        {
            // Evaluate child nodes recursively, which will cause their results to be pushed onto the stack.
            // Due to the way division works, we need to evaluate its nodes in reverse order.
            if (expression.Operator == Operator.Divide)
            {
                expression.Right.Accept(this);
                expression.Left.Accept(this);
            }
            else
            {
                expression.Left.Accept(this);
                expression.Right.Accept(this);
            }
            EmitComment("Binary operator " + expression.Operator);
            switch (expression.Operator)
            {
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                    Emit("pop", "ebx");
                    Emit("pop", "eax");
                    Emit("cmp", "eax", "ebx");
                    break;
                case Operator.Add:
                    // Pop into eax, add eax to TOS
                    Emit("pop", "eax");
                    Emit("add", "[esp]", "eax");
                    break;
                case Operator.Subtract:
                    // Pop into eax, subtract eax from TOS
                    Emit("pop", "eax");
                    Emit("sub", "[esp]", "eax");
                    break;
                case Operator.Multiply:
                    // move 0 to edx, pop into eax, multiply by TOS, move eax to TOS
                    Emit("mov", "edx", "0");
                    Emit("pop", "eax");
                    Emit("imul", "dword [esp]");
                    Emit("mov", "[esp]", "eax");
                    break;
                case Operator.Divide:
                    // Move 0 to edx, pop into eax, divide by TOS, move eax to TOS
                    Emit("mov", "edx", 0);
                    Emit("pop", "eax");
                    Emit("idiv", "dword [esp]");
                    Emit("mov", "[esp]", "eax");
                    break;
                default:
                    throw new Exception("Unsupported binary operator: " + expression.Operator);
            }
        }

        public void Visit(FunctionCallNode call)
        {
            foreach (ExpressionNode argument in Enumerable.Reverse(call.Arguments))
            {
                argument.Accept(this);
            }
            EmitComment("Function call: " + call.Name.Name);
            Emit("call", "fun_" + call.Name.Name);
            Emit("add", "esp", call.Arguments.Count * 4);
            Emit("push", "eax");
        }

        public void Visit(VariableReferenceNode reference)
        {
            EmitComment(reference.Variable.Name);
            Emit("push", GetVariableAddress(reference.SymbolTableEntry));
        }

        public void Visit(IdentifierNode identifier)
        {
            // Identifiers are handled (if necessary) by their parent node
        }

        public void Visit(StringNode str)
        {
            if (!_strings.ContainsKey(str.Value))
            {
                _strings[str.Value] = _strings.Count;
            }
            int index = _strings[str.Value];
            Emit("push", string.Format("dword [string_{0}_len]", index));
            Emit("push", string.Format("string_{0}_data", index));
        }

        public void Visit(IntegerNode integer)
        {
            EmitComment(integer.Value.ToString());
            Emit("push", "dword " + integer.Value.ToString());
        }

        public void Visit(DeclarationNode declaration)
        {
            // Declarations only have semantic meaning; no instructions are needed
        }

        public void Visit(ConstantExpressionNode constant)
        {
            constant.Value.Accept(this);
        }

        public void Visit(InputStatementNode input)
        {
            foreach (var target in input.TargetVariables)
            {
                EmitComment("Input to {0}", target.Variable.Name);
                Emit("push", "dword 0");
                Emit("push", "numCharsRead");
                Emit("push", "dword 9");
                Emit("push", "buffer");
                Emit("push", "dword [stdInHandle]");
                Emit("call", "_ReadConsoleA@20");
                //TODO: Parse int
                Emit("push", "dword [buffer]");
                Emit("pop", GetVariableAddress(target.SymbolTableEntry));
            }
        }

        public void Visit(WhileStatementNode statement)
        {
            int labelIndex = _labelCount++;
            _whileLabelIndices.Push(labelIndex);
            string conditionLabel = "while_condition_" + labelIndex;
            string endLabel = "while_end_" + labelIndex;
            EmitLabelComment("While: condition");
            EmitLabel(conditionLabel);
            var condition = RequireComparisonExpression(statement.Condition, "While");
            condition.Accept(this);
            Emit(GetJumpIfFalseInstruction(condition.Operator), endLabel);
            EmitComment("While: body");
            statement.Body.Accept(this);
            Emit("jmp", conditionLabel);
            EmitLabel(endLabel);
            _whileLabelIndices.Pop();
        }

        public void Visit(NullStatementNode statement)
        {
            if (_whileLabelIndices.Count == 0)
                throw new Exception("There is no loop to continue or break out of");

            int innermostWhileLabelIndex = _whileLabelIndices.Peek();
            if (statement.Type == NullStatementType.Continue)
            {
                EmitComment("Continue");
                Emit("jmp", "while_condition_" + innermostWhileLabelIndex);
            }
            else if (statement.Type == NullStatementType.Break)
            {
                EmitComment("Break");
                Emit("jmp", "while_end_" + innermostWhileLabelIndex);
            }
            else
            {
                throw new Exception("Unknown null statement type " + statement.Type);
            }
        }

        public void Visit(AssignmentStatementNode assignment)
        {
            assignment.Expression.Accept(this);
            EmitComment("Assign to {0}", assignment.Variable.Name);
            Emit("pop", GetVariableAddress(assignment.SymbolTableEntry));
        }

        private string GetVariableAddress(SymbolTableEntry entry)
        {
            int offset = GetStackOffset(entry);
            return string.Format("dword [ebp{0}{1}]", offset < 0 ? "" : "+", offset);
        }

        private int GetStackOffset(SymbolTableEntry entry)
        {
            /*
             * Stack layout:
             * 
             * param_n-1
             * ...
             * param_1
             * param_0
             * return_address
             *   caller_frame <- ebp
             * local_0
             * local_1
             * ...
             * local_n-1 <- esp
             */
            if (entry.Type == SymbolTableEntryType.Variable)
                return -4 * (entry.Index + 1);
            else if (entry.Type == SymbolTableEntryType.Parameter)
                return 4 * (1 + _currentFunction.LocalVariableCount - entry.Index);
            else
                throw new Exception("Unsupported symbol table entry type: " + entry.Type);
        }

        private string GetJumpIfFalseInstruction(Operator op) {
            switch (op)
            {
                case Operator.Equal:
                    return "jne";
                case Operator.NotEqual:
                    return "jeq";
                case Operator.LessThan:
                    return "jge";
                case Operator.LessThanOrEqual:
                    return "jg";
                case Operator.GreaterThanOrEqual:
                    return "jl";
                case Operator.GreaterThan:
                    return "jle";
                default:
                    throw new Exception("Not a comparison operator: " + op);
            }
        }

        private int GetStackSpaceForArguments()
        {
            return _currentFunction.LocalVariableCount * 4;
        }

        private BinaryExpressionNode RequireComparisonExpression(ExpressionNode expression, string statementType)
        {
            var condition = expression as BinaryExpressionNode;
            if (condition == null || !COMPARISON_OPERATORS.Contains(condition.Operator))
                throw new Exception(statementType + " statement must have comparison expression");
            return condition;
        }

        private void PrintInteger(IPrintItemNode item)
        {
            item.Accept(this);
            EmitComment("Print integer");
            Emit("call", "print_integer");
            Emit("add", "esp", 4);
        }

        private void PrintString(StringNode str, string comment = "Print string")
        {
            EmitComment(comment);
            Emit("push", "dword 0");
            Emit("push", "numCharsWritten");
            str.Accept(this);
            Emit("push", "dword [stdOutHandle]");
            Emit("call", "_WriteConsoleA@20");
            EmitComment("Note: _WriteConsoleA@20 seems to pop its parameters, so there is no need to adjust esp");
        }

        private void EmitLabel(string labelName)
        {
            _output.WriteLine(labelName + ":");
        }

        private void Emit(string instruction, params object[] args)
        {
            _output.WriteLine("    " + instruction.PadRight(7) + " " + string.Join(", ", args));
        }

        private void EmitHead()
        {
            _output.WriteLine(
@"global _main

extern _GetStdHandle@4
extern _ReadConsoleA@20
extern _WriteConsoleA@20
extern _ExitProcess@4

%define STDOUT_HANDLE_PARAM -11
%define STDIN_HANDLE_PARAM -10
%define BUFFER_SIZE 256

section .bss
    numCharsRead:    resd 1
    numCharsWritten: resd 1
    stdInHandle:     resd 1
    stdOutHandle:    resd 1
    buffer:          resb BUFFER_SIZE

section .text

_main:
    push    dword STDIN_HANDLE_PARAM
    call    _GetStdHandle@4
    mov     [stdInHandle], eax

    push    dword STDOUT_HANDLE_PARAM
    call    _GetStdHandle@4
    mov     [stdOutHandle], eax

    call    fun_Main

    jmp     end
");
        }

        private void EmitTail()
        {
            _output.WriteLine(@"
print_integer:
    push    ebp
    mov     ebp, esp
    mov     eax, [ebp+8]        ; Load parameter into eax
    mov     ebx, buffer+BUFFER_SIZE ; Pointer into output buffer
    cmp     eax, 0              ; Is the number nonnegative?
    jge     print_integer_loop  ; Skip the negation if it is
    neg     eax                 ; Negate the number to make it positive
print_integer_loop:
    mov     edx, 0              ; Set up for division
    mov     ecx, 10             ; Denominator
    idiv    ecx                 ; Divide
    add     edx, '0'            ; Turn the modulo into an ASCII digit
    dec     ebx                 ; Move one step left in the output buffer
    mov     [ebx], dl           ; Copy digit to output buffer
    cmp     eax, 0              ; Loop if there is anything left of the number
    jne     print_integer_loop  ; -'-
    cmp     [ebp+8], dword 0    ; Is the number nonnegative?
    jge     print_integer_sign_done ; Skip the sign if it is
    dec     ebx                 ; Move one step left in the output buffer
    mov     [ebx], byte '-'     ; Copy minus sign to output buffer
print_integer_sign_done:
    push    dword 0             ; Param 4 for _WriteConsoleA@20: Unused
    push    numCharsWritten     ; Param 3 for _WriteConsoleA@20: Pointer to number of characters written
    push    buffer+BUFFER_SIZE  ; Param 2 for _WriteConsoleA@20: Number of characters to write
    sub     [esp], ebx          ; -'-
    push    ebx                     ; Param 1 for _WriteConsoleA@20: Start of buffer to write
    push    dword [stdOutHandle]    ; Param 0 for _WriteConsoleA@20: Handle to file descriptor to write to
    call    _WriteConsoleA@20       ; Make the call to _WriteConsoleA@20
    leave                       ; Note that _WriteConsoleA@20 is popping its parameters, so there's no need to adjust esp (and we're about to return anyway)
    ret

end:
    call    _ExitProcess@4      ; Exit, and use TOS (the return value from Main) as the exit code

section .data
    str:    db 'Hello world!',0x0d,0x0a
    strLen: equ $-str");
            var stringEntries = _strings.OrderBy(e => e.Value);
            foreach (var entry in stringEntries)
            {
                string cleaned = entry.Key
                    .Replace("`", @"\`")
                    .Replace("\n", @"\n");
                _output.WriteLine(("string_" + entry.Value + "_data: ").PadRight(20) + "db `" + cleaned + "`");
            }
            foreach (var entry in stringEntries)
            {
                _output.WriteLine(("string_" + entry.Value + "_len: ").PadRight(20) + "dd " + entry.Key.Length);
            }
        }

        private void EmitBlankLine()
        {
            _output.WriteLine();
        }

        private void EmitLabelComment(string format, params object[] args)
        {
            _output.WriteLine("; " + format, args);
        }

        private void EmitComment(string format, params object[] args)
        {
            _output.WriteLine("    ; " + format, args);
        }
    }
}
