using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Compiler
{
    public class JavaScriptCodeGenerator
        : ICodeGenerator, ISyntaxNodeVisitor
    {
        private readonly TextWriter _output;
        private int _indentationLevel = 0;

        public JavaScriptCodeGenerator(TextWriter output)
        {
            _output = output;
        }

        public void Generate(ProgramNode program)
        {
            Visit(program);
        }

        public void Visit(ProgramNode program)
        {
            Emit("var stack = [];");
            foreach (var function in program.Functions)
            {
                function.Accept(this);
            }
            Emit("Main();");
        }

        public void Visit(FunctionNode function)
        {
            Emit("function {0}() {{", function.Name.Name);
            ++_indentationLevel;
            Emit("var locals = [];");
            Emit("var params = [];");
            for (int i = function.Parameters.Count - 1; i >= 0; --i)
            {
                Emit("params[{0}] = stack.pop();", i);
            }
            function.Body.Accept(this);
            --_indentationLevel;
            Emit("}");
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
                Emit("locals[{0}] = stack.pop();", assignment.SymbolTableEntry.Index);
            else
                Emit("params[{0}] = stack.pop();", assignment.SymbolTableEntry.Index);
        }

        public void Visit(ReturnStatementNode statement)
        {
            statement.Expression.Accept(this);
            Emit("return;");
        }

        public void Visit(NullStatementNode statement)
        {
            
        }

        public void Visit(IfStatementNode statement)
        {
            statement.Condition.Accept(this);
            Emit("if (stack.pop()) {");
            ++_indentationLevel;
            statement.ThenBody.Accept(this);
            --_indentationLevel;
            Emit("}");
            if (statement.ElseBody != null)
            {
                Emit("else {");
                ++_indentationLevel;
                statement.ElseBody.Accept(this);
                --_indentationLevel;
                Emit("}");
            }
        }

        public void Visit(WhileStatementNode statement)
        {
            Emit("do {");
            ++_indentationLevel;
            statement.Condition.Accept(this);
            Emit("if (!stack.pop()) break;");
            statement.Body.Accept(this);
            --_indentationLevel;
            Emit("} while (1);");
        }

        public void Visit(PrintStatementNode print)
        {
            var reversedItems = ((IEnumerable<IPrintItemNode>) print.Items).Reverse();
            foreach (var item in reversedItems)
            {
                item.Accept(this);
            }
            var parameters = string.Join(" + ", Enumerable.Repeat("stack.pop()", print.Items.Count));
            Emit("console.log(\"\" + {0});", parameters);
        }

        public void Visit(InputStatementNode input)
        {
            string pattern = "{0}[{1}] = +prompt(\"Please enter an integer:\");";
            foreach (var target in input.TargetVariables)
            {
                if (target.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                    Emit(pattern, "locals", target.SymbolTableEntry.Index);
                else
                    Emit(pattern, "params", target.SymbolTableEntry.Index);
            }
        }

        public void Visit(BinaryExpressionNode expression)
        {
            expression.Right.Accept(this);
            expression.Left.Accept(this);
            string op;
            bool comparison = false;
            switch (expression.Operator)
            {
                case Operator.Equal:
                    op = "===";
                    comparison = true;
                    break;
                case Operator.NotEqual:
                    op = "!==";
                    comparison = true;
                    break;
                case Operator.LessThan:
                    op = "<";
                    comparison = true;
                    break;
                case Operator.LessThanOrEqual:
                    op = "<=";
                    comparison = true;
                    break;
                case Operator.GreaterThan:
                    op = ">";
                    comparison = true;
                    break;
                case Operator.GreaterThanOrEqual:
                    op = ">=";
                    comparison = true;
                    break;
                case Operator.Add:
                    op = "+";
                    break;
                case Operator.Subtract:
                    op = "-";
                    break;
                case Operator.Multiply:
                    op = "*";
                    break;
                case Operator.Divide:
                    op = "/";
                    break;
                default:
                    throw new Exception("Unsupported binary operator: " + expression.Operator);
            }
            if (comparison)
                Emit("stack.push(stack.pop() {0} stack.pop() ? 1 : 0);", op);
            else if (expression.Operator == Operator.Divide)
                Emit("stack.push(Math.floor(stack.pop() {0} stack.pop()));", op);
            else
                Emit("stack.push(stack.pop() {0} stack.pop());", op);
        }

        public void Visit(UnaryExpressionNode expression)
        {
            
        }

        public void Visit(FunctionCallNode call)
        {
            foreach (var arg in call.Arguments)
            {
                arg.Accept(this);
            }
            Emit("{0}();", call.Name.Name);
        }

        public void Visit(ConstantExpressionNode constant)
        {
            constant.Value.Accept(this);
        }

        public void Visit(VariableReferenceNode reference)
        {
            EmitComment("Read {0}", reference.Variable.Name);
            if (reference.SymbolTableEntry.Type == SymbolTableEntryType.Variable)
                Emit("stack.push(locals[{0}]);", reference.SymbolTableEntry.Index);
            else
                Emit("stack.push(params[{0}]);", reference.SymbolTableEntry.Index);
        }

        public void Visit(DeclarationNode declaration)
        {
            
        }

        public void Visit(IdentifierNode identifier)
        {
            
        }

        public void Visit(IntegerNode integer)
        {
            EmitComment("Constant integer {0}", integer.Value);
            Emit("stack.push({0});", integer.Value);
        }

        public void Visit(StringNode str)
        {
            Emit("stack.push(\"{0}\");", str.Value);
        }

        private void Emit(string code)
        {
            Emit("{0}", code);
        }

        private void Emit(string pattern, params object[] args)
        {
            _output.WriteLine(new string(' ', 4 * _indentationLevel) + pattern, args);
        }

        private void EmitComment(string comment)
        {
            EmitComment("{0}", comment);
        }

        private void EmitComment(string comment, params object[] args)
        {
            Emit("// " + comment, args);
        }
    }
}
