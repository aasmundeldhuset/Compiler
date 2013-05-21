using System;
using System.IO;
using System.Linq;

namespace Compiler
{
    public class MsilCodeGenerator : ICodeGenerator
    {
        private readonly TextWriter _output;
        private int _labelCount;
        private int? _innermostWhileLabelIndex;

        public MsilCodeGenerator(TextWriter output)
        {
            _output = output;
        }

        public void Generate(ProgramNode program)
        {
            _labelCount = 0;
            _innermostWhileLabelIndex = null;
            EmitHead();
            GenerateRecursively(program);
            EmitTail();
        }

        private void GenerateRecursively(ISyntaxNode node)
        {
            if (node is ProgramNode)
            {
                var program = (ProgramNode) node;
                foreach (var function in program.Functions)
                {
                    GenerateRecursively(function);
                }
            }
            else if (node is FunctionNode)
            {
                var function = (FunctionNode) node;
                string parameterTypes = string.Join(", ", Enumerable.Repeat("int32", function.Parameters.Count));
                _output.WriteLine("        .method public static int32 {0}({1}) cil managed {{", function.Name.Name, parameterTypes);
                if (function.Name.Name == "Main")
                    Emit(".entrypoint");
                if (function.LocalVariableCount > 0)
                {
                    var variableTypes = string.Join(", ", Enumerable.Repeat("int32", function.LocalVariableCount));
                    Emit(string.Format(".locals init ({0})", variableTypes));
                }
                GenerateRecursively(function.Body);
                _output.WriteLine("        }");
            }
            else if (node is IntegerNode)
            {
                var integer = (IntegerNode) node;
                EmitComment("{0}", integer.Value);
                Emit("ldc.i4", integer.Value);
            }
            else if (node is AssignmentStatementNode)
            {
                var assign = (AssignmentStatementNode) node;
                GenerateRecursively(assign.Expression);
                EmitComment("Assign to {0}", assign.Variable.Name);
                if (assign.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                    Emit("stloc", assign.SymbolTableEntry.Index);
                else // Parameter
                    Emit("starg", assign.SymbolTableEntry.Index);
            }
            else if (node is VariableReferenceNode)
            {
                var reference = (VariableReferenceNode) node;
                EmitComment("{0}", reference.Variable.Name);
                if (reference.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                    Emit("ldloc", reference.SymbolTableEntry.Index);
                else // Parameter
                    Emit("ldarg", reference.SymbolTableEntry.Index);
            }
            else if (node is ReturnStatementNode)
            {
                var ret = (ReturnStatementNode) node;
                GenerateRecursively(ret.Expression);
                EmitComment("Return");
                Emit("ret");
            }
            else if (node is IfStatementNode)
            {
                var statement = (IfStatementNode) node;
                int labelIndex = _labelCount++;
                string falseLabel = "if_false_" + labelIndex;
                string endLabel = "if_end_" + labelIndex;
                EmitComment("If: condition");
                GenerateRecursively(statement.Condition);
                EmitComment("If: jump");
                Emit("brfalse", falseLabel);
                EmitComment("If: begin true part");
                GenerateRecursively(statement.ThenBody);
                if (statement.ElseBody != null)
                    Emit("br", endLabel);
                EmitComment("If: end true part");
                EmitLabel(falseLabel);
                if (statement.ElseBody != null)
                {
                    EmitComment("If: begin false part");
                    GenerateRecursively(statement.ElseBody);
                    EmitComment("If: end false part");
                    EmitLabel(endLabel);
                }
            }
            else if (node is WhileStatementNode)
            {
                var statement = (WhileStatementNode) node;
                int labelIndex = _labelCount++;
                int? previousInnermostWhileLabelIndex = _innermostWhileLabelIndex;
                _innermostWhileLabelIndex = labelIndex;
                string conditionLabel = "while_condition_" + labelIndex;
                string endLabel = "while_end_" + labelIndex;
                EmitComment("While: condition");
                EmitLabel(conditionLabel);
                GenerateRecursively(statement.Condition);
                Emit("brfalse", endLabel);
                EmitComment("While: body");
                GenerateRecursively(statement.Body);
                Emit("br", conditionLabel);
                EmitLabel(endLabel);
                _innermostWhileLabelIndex = previousInnermostWhileLabelIndex;
            }
            else if (node is NullStatementNode)
            {
                var statement = (NullStatementNode) node;
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
            else if (node is BinaryExpressionNode)
            {
                var expr = (BinaryExpressionNode) node;
                if (new[] {Operator.NotEqual, Operator.LessThanOrEqual, Operator.GreaterThanOrEqual}.Contains(expr.Operator))
                {
                    EmitComment("Prepare for binary operator " + expr.Operator);
                    Emit("ldc.i4", 1);
                }
                GenerateRecursively(expr.Left);
                GenerateRecursively(expr.Right);
                EmitComment("Binary operator " + expr.Operator);
                switch (expr.Operator)
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
                        throw new Exception("Unsupported binary operator: " + expr.Operator);
                }
            }
            else if (node is UnaryExpressionNode)
            {
                var expr = (UnaryExpressionNode) node;
                GenerateRecursively(expr.Child);
                EmitComment("Unary operator " + expr.Operator);
                if (expr.Operator == Operator.Subtract)
                    Emit("neg");
                else
                    throw new Exception("Unsupported unary operator: " + expr.Operator);
            }
            else if (node is ConstantExpressionNode)
            {
                var constant = (ConstantExpressionNode) node;
                GenerateRecursively(constant.Value);
            }
            else if (node is FunctionCallNode)
            {
                var call = (FunctionCallNode) node;
                foreach (var argument in call.Arguments)
                {
                    GenerateRecursively(argument);
                }
                string argumentTypes = string.Join(", ", Enumerable.Repeat("int32", call.Arguments.Count));
                string signature = string.Format("int32 Vsl.VslMain::{0}({1})", call.Name.Name, argumentTypes);
                EmitComment("Call function " + call.Name.Name);
                Emit("call", signature);
            }
            else if (node is BlockStatementNode)
            {
                var block = (BlockStatementNode) node;
                EmitComment("Begin block");
                foreach (var statement in block.Statements)
                {
                    GenerateRecursively(statement);
                }
                EmitComment("End block");
            }
            else if (node is StringNode)
            {
                var str = (StringNode) node;
                EmitComment('"' + str.Value + '"');
                Emit("ldstr", '"' + str.Value + '"');
            }
            else if (node is PrintStatementNode)
            {
                var print = (PrintStatementNode) node;
                foreach (var item in print.Items)
                {
                    GenerateRecursively(item);
                    EmitComment("Print one item");
                    if (item is StringNode)
                        Emit("call", "void [mscorlib]System.Console::Write(string)");
                    else
                        Emit("call", "void [mscorlib]System.Console::Write(int32)");
                }
                EmitComment("Done printing");
                Emit("call", "void [mscorlib]System.Console::WriteLine()");
            }
            else if (node is InputStatementNode)
            {
                var input = (InputStatementNode) node;
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
            else
            {
                throw new Exception(string.Format("Unknown node type {0} (shouldn't happen)", node.GetType().FullName));
            }
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
