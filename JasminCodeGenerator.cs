using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compiler
{
    public class JasminCodeGenerator : ICodeGenerator, ISyntaxNodeVisitor
    {
        private readonly TextWriter _output;
        private int _labelCount;
        private int? _innermostWhileLabelIndex;
        private readonly Stack<int> _parameterCounts = new Stack<int>();
        private bool _currentFunctionIsMain;

        public JasminCodeGenerator(TextWriter output)
        {
            _output = output;
        }

        public void Generate(ProgramNode program)
        {
            Visit(program);
        }
        
        public void Visit(ProgramNode program)
        {
            _labelCount = 0;
            _innermostWhileLabelIndex = null;
            EmitHead();
            foreach (var function in program.Functions)
            {
                function.Accept(this);
            }
        }

        public void Visit(FunctionNode function)
        {
            const string stackLimit = ".limit stack 256"; //TODO: Arbitrary limit - should be determined by analyzing the depth of the expressions in the method body
            string localsLimit = string.Format(".limit locals {0}", function.Parameters.Count + function.LocalVariableCount);
            _currentFunctionIsMain = (function.Name.Name == "Main");
            if (_currentFunctionIsMain)
            {
                _output.WriteLine(".method public static main([Ljava/lang/String;)V");
                Emit(stackLimit);
                if (function.LocalVariableCount > 0)
                    Emit(localsLimit);
                Emit("new java/util/Scanner");
                Emit("dup");
                Emit("getstatic", "java/lang/System/in", "Ljava/io/InputStream;");
                Emit("invokespecial java/util/Scanner/<init>(Ljava/io/InputStream;)V");
                Emit("putstatic VslMain/scanner Ljava/util/Scanner;");
            }
            else
            {
                var parameterTypes = new string('I', function.Parameters.Count);
                _output.WriteLine(".method public static {0}({1})I", function.Name.Name, parameterTypes);
                Emit(stackLimit);
                if (function.LocalVariableCount > 0)
                    Emit(localsLimit);
            }
            _parameterCounts.Push(function.Parameters.Count);
            function.Body.Accept(this);
            _parameterCounts.Pop();
            _output.WriteLine(".end method");
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

        public void Visit(AssignmentStatementNode assignment)
        {
            assignment.Expression.Accept(this);
            EmitComment("Assign to {0}", assignment.Variable.Name);
            Emit("istore", GetLocalsIndex(assignment.SymbolTableEntry));
        }

        public void Visit(ReturnStatementNode statement)
        {
            statement.Expression.Accept(this);
            EmitComment("Return");
            if (_currentFunctionIsMain)
            {
                Emit("pop");
                Emit("return");
            }
            else
            {
                Emit("ireturn");
            }
        }

        public void Visit(NullStatementNode statement)
        {
            if (_innermostWhileLabelIndex == null)
                throw new Exception("There is no loop to continue or break out of");
            if (statement.Type == NullStatementType.Continue)
            {
                EmitComment("Continue");
                Emit("goto", "while_condition_" + _innermostWhileLabelIndex);
            }
            else if (statement.Type == NullStatementType.Break)
            {
                EmitComment("Break");
                Emit("goto", "while_end_" + _innermostWhileLabelIndex);
            }
            else
            {
                throw new Exception("Unknown null statement type " + statement.Type);
            }
        }

        public void Visit(IfStatementNode statement)
        {
            int labelIndex = _labelCount++;
            string falseLabel = "if_false_" + labelIndex;
            string endLabel = "if_end_" + labelIndex;
            EmitComment("If: condition");
            statement.Condition.Accept(this);
            EmitComment("If: jump");
            Emit("ifeq", falseLabel);
            EmitComment("If: begin true part");
            statement.ThenBody.Accept(this);
            if (statement.ElseBody != null)
                Emit("goto", endLabel);
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

        public void Visit(WhileStatementNode statement)
        {
            int labelIndex = _labelCount++;
            int? previousInnermostWhileLabelIndex = _innermostWhileLabelIndex;
            _innermostWhileLabelIndex = labelIndex;
            string conditionLabel = "while_condition_" + labelIndex;
            string endLabel = "while_end_" + labelIndex;
            EmitComment("While: condition");
            EmitLabel(conditionLabel);
            statement.Condition.Accept(this);
            Emit("ifeq", endLabel);
            EmitComment("While: body");
            statement.Body.Accept(this);
            Emit("goto", conditionLabel);
            EmitLabel(endLabel);
            _innermostWhileLabelIndex = previousInnermostWhileLabelIndex;
        }

        public void Visit(PrintStatementNode print)
        {
            foreach (var item in print.Items)
            {
                EmitComment("Print one item");
                Emit("getstatic", "java/lang/System/out", "Ljava/io/PrintStream;");
                item.Accept(this);
                if (item is StringNode)
                    Emit("invokevirtual", "java/io/PrintStream/print(Ljava/lang/String;)V");
                else
                    Emit("invokevirtual", "java/io/PrintStream/print(I)V");
            }
            EmitComment("Done printing");
            Emit("getstatic", "java/lang/System/out", "Ljava/io/PrintStream;");
            Emit("invokevirtual", "java/io/PrintStream/println()V");
        }

        public void Visit(InputStatementNode input)
        {
            foreach (var target in input.TargetVariables)
            {
                EmitComment("Input to " + target.Variable.Name);
                Emit("getstatic", "VslMain/scanner", "Ljava/util/Scanner;");
                Emit("invokevirtual", "java/util/Scanner/nextInt()I");
                Emit("istore", GetLocalsIndex(target.SymbolTableEntry));
            }
        }

        public void Visit(BinaryExpressionNode expression)
        {
            string trueLabel = "cmp_true_" + _labelCount++;
            bool isComparison = new[] {Operator.Equal, Operator.NotEqual, Operator.LessThan, Operator.LessThanOrEqual, Operator.GreaterThan, Operator.GreaterThanOrEqual}.Contains(expression.Operator);
            if (isComparison)
                Emit("ldc", "0");
            expression.Left.Accept(this);
            expression.Right.Accept(this);
            EmitComment("Binary operator " + expression.Operator);
            switch (expression.Operator)
            {
                case Operator.Equal:
                    Emit("if_icmpne", trueLabel);
                    break;
                case Operator.NotEqual:
                    Emit("if_icmpeq", trueLabel);
                    break;
                case Operator.LessThan:
                    Emit("if_icmpge", trueLabel);
                    break;
                case Operator.LessThanOrEqual:
                    Emit("if_icmpgt", trueLabel);
                    break;
                case Operator.GreaterThan:
                    Emit("if_icmple", trueLabel);
                    break;
                case Operator.GreaterThanOrEqual:
                    Emit("if_icmplt", trueLabel);
                    break;
                case Operator.Add:
                    Emit("iadd");
                    break;
                case Operator.Subtract:
                    Emit("isub");
                    break;
                case Operator.Multiply:
                    Emit("imul");
                    break;
                case Operator.Divide:
                    Emit("idiv");
                    break;
                default:
                    throw new Exception("Unsupported binary operator: " + expression.Operator);
            }
            if (isComparison)
            {
                Emit("ldc", "1");
                Emit("iadd");
                EmitLabel(trueLabel);
            }
        }

        public void Visit(UnaryExpressionNode expression)
        {
            expression.Child.Accept(this);
            EmitComment("Unary operator " + expression.Operator);
            if (expression.Operator == Operator.Subtract)
                Emit("ineg");
            else
                throw new Exception("Unsupported unary operator: " + expression.Operator);
        }

        public void Visit(FunctionCallNode call)
        {
            foreach (var argument in call.Arguments)
            {
                argument.Accept(this);
            }
            var argumentTypes = new string('I', call.Arguments.Count);
            string signature = string.Format("VslMain/{0}({1})I", call.Name.Name, argumentTypes);
            EmitComment("Call function " + call.Name.Name);
            Emit("invokestatic", signature);
        }

        public void Visit(ConstantExpressionNode constant)
        {
            constant.Value.Accept(this);
        }

        public void Visit(VariableReferenceNode reference)
        {
            EmitComment("{0}", reference.Variable.Name);
            Emit("iload", GetLocalsIndex(reference.SymbolTableEntry));
        }

        public void Visit(DeclarationNode declaration)
        {
            // Declarations only have semantic meaning; no instructions are needed
        }

        public void Visit(IdentifierNode identifier)
        {
            // Identifiers are handled (if necessary) by their parent node
        }

        public void Visit(IntegerNode integer)
        {
            EmitComment("{0}", integer.Value);
            Emit("ldc", integer.Value);
        }

        public void Visit(StringNode str)
        {
            EmitComment('"' + str.Value + '"');
            Emit("ldc", '"' + str.Value + '"');
        }

        private void Emit(string instruction, params object[] args)
        {
            _output.WriteLine("    " + string.Join("\t", new[] {instruction}.Concat(args)));
        }

        private void EmitLabel(string labelName)
        {
            _output.WriteLine(labelName + ":");
        }

        private void EmitHead()
        {
            _output.WriteLine(
@".class public VslMain
.super java/lang/Object
.field public static scanner Ljava/util/Scanner;");
        }

        private void EmitComment(string format, params object[] args)
        {
            _output.WriteLine("    ; " + format, args);
        }

        private int GetLocalsIndex(SymbolTableEntry entry)
        {
            if (entry.Type == SymbolTableEntryType.Parameter)
                return entry.Index;
            return _parameterCounts.Peek() + entry.Index;
        }
    }
}
