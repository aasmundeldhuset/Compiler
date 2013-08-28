using System;
using System.IO;
using System.Linq;

namespace Compiler
{
    public class CilCodeGenerator : ICodeGenerator, ISyntaxNodeVisitor
    {
        private readonly TextWriter _output;
        private int _labelCount;
        private int? _innermostWhileLabelIndex;

        public CilCodeGenerator(TextWriter output)
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
            EmitTail();
        }

        public void Visit(FunctionNode function)
        {
            string parameterTypes = string.Join(", ", Enumerable.Repeat("int32", function.Parameters.Count));
            _output.WriteLine("        .method public static int32 {0}({1}) cil managed {{", function.Name.Name, parameterTypes);
            if (function.Name.Name == "Main")
                Emit(".entrypoint");
            if (function.LocalVariableCount > 0)
            {
                var variableTypes = string.Join(", ", Enumerable.Repeat("int32", function.LocalVariableCount));
                Emit(string.Format(".locals init ({0})", variableTypes));
            }
            function.Body.Accept(this);
            _output.WriteLine("        }");
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
            if (assignment.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                Emit("stloc", assignment.SymbolTableEntry.Index);
            else // Parameter
                Emit("starg", assignment.SymbolTableEntry.Index);
        }

        public void Visit(ReturnStatementNode statement)
        {
            statement.Expression.Accept(this);
            EmitComment("Return");
            Emit("ret");
        }

        public void Visit(NullStatementNode statement)
        {
            if (_innermostWhileLabelIndex == null)
                throw new Exception("There is no loop to continue or break out of");
            if (statement.Type == NullStatementType.Continue)
            {
                EmitComment("Continue");
                Emit("br", "while_cond_" + _innermostWhileLabelIndex);
            }
            else if (statement.Type == NullStatementType.Break)
            {
                EmitComment("Break");
                Emit("br", "while_end_" + _innermostWhileLabelIndex);
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
            Emit("brfalse", falseLabel);
            EmitComment("If: begin true part");
            statement.ThenBody.Accept(this);
            if (statement.ElseBody != null)
                Emit("br", endLabel);
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
            Emit("brfalse", endLabel);
            EmitComment("While: body");
            statement.Body.Accept(this);
            Emit("br", conditionLabel);
            EmitLabel(endLabel);
            _innermostWhileLabelIndex = previousInnermostWhileLabelIndex;
        }

        public void Visit(PrintStatementNode print)
        {
            foreach (var item in print.Items)
            {
                item.Accept(this);
                EmitComment("Print one item");
                if (item is StringNode)
                    Emit("call", "void [mscorlib]System.Console::Write(string)");
                else
                    Emit("call", "void [mscorlib]System.Console::Write(int32)");
            }
            EmitComment("Done printing");
            Emit("call", "void [mscorlib]System.Console::WriteLine()");
        }

        public void Visit(InputStatementNode input)
        {
            foreach (var target in input.TargetVariables)
            {
                EmitComment("Input to " + target.Variable.Name);
                Emit("call", "string [mscorlib]System.Console::ReadLine()");
                Emit("call", "int32 [mscorlib]System.Int32::Parse(string)");
                if (target.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                    Emit("stloc", target.SymbolTableEntry.Index);
                else // Parameter
                    Emit("starg", target.SymbolTableEntry.Index);
            }
        }

        public void Visit(BinaryExpressionNode expression)
        {
            if (new[] {Operator.NotEqual, Operator.LessThanOrEqual, Operator.GreaterThanOrEqual}.Contains(expression.Operator))
            {
                EmitComment("Prepare for binary operator " + expression.Operator);
                Emit("ldc.i4", 1);
            }
            expression.Left.Accept(this);
            expression.Right.Accept(this);
            EmitComment("Binary operator " + expression.Operator);
            switch (expression.Operator)
            {
                case Operator.Equal:
                    Emit("ceq");
                    break;
                case Operator.NotEqual:
                    Emit("ceq");
                    Emit("sub");
                    break;
                case Operator.LessThan:
                    Emit("clt");
                    break;
                case Operator.LessThanOrEqual:
                    Emit("cgt");
                    Emit("sub");
                    break;
                case Operator.GreaterThan:
                    Emit("cgt");
                    break;
                case Operator.GreaterThanOrEqual:
                    Emit("clt");
                    Emit("sub");
                    break;
                case Operator.Add:
                    Emit("add");
                    break;
                case Operator.Subtract:
                    Emit("sub");
                    break;
                case Operator.Multiply:
                    Emit("mul");
                    break;
                case Operator.Divide:
                    Emit("div");
                    break;
                default:
                    throw new Exception("Unsupported binary operator: " + expression.Operator);
            }
        }

        public void Visit(UnaryExpressionNode expression)
        {
            expression.Child.Accept(this);
            EmitComment("Unary operator " + expression.Operator);
            if (expression.Operator == Operator.Subtract)
                Emit("neg");
            else
                throw new Exception("Unsupported unary operator: " + expression.Operator);
        }

        public void Visit(FunctionCallNode call)
        {
            foreach (var argument in call.Arguments)
            {
                argument.Accept(this);
            }
            string argumentTypes = string.Join(", ", Enumerable.Repeat("int32", call.Arguments.Count));
            string signature = string.Format("int32 Vsl.VslMain::{0}({1})", call.Name.Name, argumentTypes);
            EmitComment("Call function " + call.Name.Name);
            Emit("call", signature);
        }

        public void Visit(ConstantExpressionNode constant)
        {
            constant.Value.Accept(this);
        }

        public void Visit(VariableReferenceNode reference)
        {
            EmitComment("{0}", reference.Variable.Name);
            if (reference.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                Emit("ldloc", reference.SymbolTableEntry.Index);
            else // Parameter
                Emit("ldarg", reference.SymbolTableEntry.Index);
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
            Emit("ldc.i4", integer.Value);
        }

        public void Visit(StringNode str)
        {
            EmitComment('"' + str.Value + '"');
            Emit("ldstr", '"' + str.Value + '"');
        }

        private void Emit(string instruction, params object[] args)
        {
            _output.WriteLine("            " + string.Join("\t", new[] {instruction}.Concat(args)));
        }

        private void EmitLabel(string labelName)
        {
            _output.WriteLine(labelName + ":");
        }

        private void EmitHead()
        {
            _output.WriteLine(
@".assembly extern mscorlib {}
.assembly VSL {}
.module VSL.exe

.namespace Vsl {
    .class public auto ansi VslMain extends [mscorlib]System.Object {");
        }

        private void EmitTail()
        {
            _output.WriteLine(
@"    }
}");
        }

        private void EmitComment(string format, params object[] args)
        {
            _output.WriteLine("            // " + format, args);
        }
    }
}
